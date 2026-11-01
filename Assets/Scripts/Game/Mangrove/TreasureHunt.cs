using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pirate treasure on the Mangrove island (docs/banana-island.md): a pirate falls and drops a torn map (or the lagoon
/// gives up a soggy one), an X appears on a beach or in the shallows, and the finder goes to dig. Anyone else on the
/// island joins with !dig. Every digger standing at the X adds to the dig; when it is done a chest comes up, and the
/// server pays everyone who dug (api/mangrove/treasure/chest): coins and a roll on the island's treasure table. One
/// map at a time, and a while between maps.
/// </summary>
public sealed class TreasureHunt : MonoBehaviour
{
    public const float PirateMapChance = 1f / 120f;
    public const float FishMapChance = 1f / 900f;
    public const float Cooldown = 300f;
    /// <summary>Digger seconds at the X it takes: one digger 40 s, four 10 s.</summary>
    public const float DigWork = 40f;
    public const float GiveUpAfter = 180f;
    private const float ReachDistance = 2.2f;
    private const int MostDiggers = 50;
    private const float ChestSeconds = 25f;

    private static TreasureHunt current;
    private static float nextMap;

    private IslandController island;
    private Vector3 spot;
    private PlayerController finder;
    private readonly List<PlayerController> diggers = new List<PlayerController>();
    private readonly HashSet<PlayerController> digging = new HashSet<PlayerController>();
    private float work;
    private float startedAt;
    private bool dug;
    private GameObject mark;
    private GameObject chest;
    private float chestRise;

    public static bool Running => current && !current.dug;

    // ---- Finding a map -----------------------------------------------------------------------------

    /// <summary>A pirate fell on the banana island: now and then it carried a map.</summary>
    public static void PirateDefeated(PlayerController killer, EnemyController enemy)
    {
        if (!killer || killer.IsBot && !Application.isEditor) return;
        if (UnityEngine.Random.value > PirateMapChance) return;
        TryBegin(killer, Localization.UI_TREASURE_FOUND_PIRATE, Localization.MSG_TREASURE_FOUND_PIRATE);
    }

    /// <summary>A fish landed in the banana island's lagoon: very rarely a bottle with a map in it.</summary>
    public static void FishedUp(PlayerController fisher)
    {
        if (!fisher) return;
        if (UnityEngine.Random.value > FishMapChance) return;
        TryBegin(fisher, Localization.UI_TREASURE_FOUND_FISH, Localization.MSG_TREASURE_FOUND_FISH);
    }

    /// <summary>Starts a hunt from this player's map (also for testing); false while another runs or too soon after one.</summary>
    public static bool TryBegin(PlayerController finder, string feedLine = null, string chatLine = null, bool force = false)
    {
        if (!finder || !finder.Island || finder.Island.Island != IslandController.MangroveIsland || finder.Island.Locked) return false;
        if (current || (!force && Time.time < nextMap)) return false;
        if (PirateRaid.Running || Regatta.Running) return false;
        if (!PickSpot(finder.Island, finder.Position, out var spot)) return false;

        var go = new GameObject("Treasure Hunt");
        go.transform.SetParent(finder.Island.transform, true);
        current = go.AddComponent<TreasureHunt>();
        current.island = finder.Island;
        current.spot = spot;
        current.finder = finder;
        current.startedAt = Time.time;
        current.MakeMark();
        current.Join(finder);

        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, feedLine ?? Localization.UI_TREASURE_FOUND_PIRATE, finder.Name), "");
        GameManager.Instance?.RavenBot?.Announce(chatLine ?? Localization.MSG_TREASURE_FOUND_PIRATE, finder.Name);
        return true;
    }

    /// <summary>
    /// Somewhere on the island's navmesh away from the finder: a beach, a boardwalk or the lagoon's shallows. Tries a
    /// few rings round the island's middle.
    /// </summary>
    private static bool PickSpot(IslandController island, Vector3 from, out Vector3 spot)
    {
        var mask = NavMeshHelper.AreaMaskAt(from);
        var centre = island.transform.position;
        var spawn = island.SpawnPosition;
        if (spawn != Vector3.zero) centre = spawn;
        for (var i = 0; i < 40; ++i)
        {
            var at = centre + Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward * UnityEngine.Random.Range(8f, 45f);
            if (!NavMesh.SamplePosition(at, out var hit, 5f, mask)) continue;
            if ((hit.position - from).sqrMagnitude < 12f * 12f) continue;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, hit.position, mask, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            spot = hit.position;
            return true;
        }
        spot = default;
        return false;
    }

    // ---- !dig --------------------------------------------------------------------------------------

    public enum JoinResult { Joined, Already, NoHunt, NotOnIsland, Busy, Full }

    /// <summary>!dig: a player on the banana island joins the dig.</summary>
    public static JoinResult JoinCurrent(PlayerController player)
    {
        if (!Running) return JoinResult.NoHunt;
        if (!player || player.Island != current.island) return JoinResult.NotOnIsland;
        if (current.diggers.Contains(player)) return JoinResult.Already;
        if (current.diggers.Count >= MostDiggers) return JoinResult.Full;
        if (player.Controlled || player.Stats.IsDead || player.InRavebridge) return JoinResult.Busy;
        current.Join(player);
        return JoinResult.Joined;
    }

    /// <summary>The streamer clicked the X: how to help.</summary>
    public static void Clicked()
    {
        if (!Running) return;
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_HOW, current.diggers.Count), "");
    }

    private void Join(PlayerController player)
    {
        diggers.Add(player);
        var index = diggers.Count - 1;
        player.BeginErrand(p => Dig(p, index));
    }

    /// <summary>A digger's errand: to their place round the X, then digging until the chest is up.</summary>
    private bool Dig(PlayerController p, int index)
    {
        if (dug || !this || p.Island != island)
        {
            StopDigging(p);
            return false;
        }

        // spread round the X so they do not stand in one another
        var angle = index * 137.5f;
        var place = spot + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (1.1f + 0.35f * (index / 6));
        if ((p.Position - spot).sqrMagnitude > ReachDistance * ReachDistance * (1f + index / 6))
        {
            if (digging.Remove(p)) p.Animations.EndFarming();
            if (!p.Movement.IsMoving || (p.agent && (p.agent.destination - place).sqrMagnitude > 1f)) p.SetDestination(place);
            return true;
        }

        if (digging.Add(p))
        {
            if (p.agent && p.agent.isOnNavMesh) p.agent.ResetPath();
            var look = spot - p.Position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.LookRotation(look);
            p.Animations.StartFarming();
        }
        return true;
    }

    private void StopDigging(PlayerController p)
    {
        if (p && digging.Remove(p)) p.Animations.EndFarming();
    }

    // ---- The dig -----------------------------------------------------------------------------------

    private void Update()
    {
        if (!dug)
        {
            digging.RemoveWhere(p => !p || p.Island != island);
            work += digging.Count * Time.deltaTime;
            if (work >= DigWork) Unearth();
            else if (Time.time - startedAt > GiveUpAfter && digging.Count == 0) End(false);
            return;
        }

        // the chest comes up out of the sand, then goes after a while
        if (chest)
        {
            chestRise = Mathf.MoveTowards(chestRise, 1f, Time.deltaTime * 0.8f);
            chest.transform.position = spot + Vector3.up * Mathf.Lerp(-0.9f, 0.02f, Mathf.SmoothStep(0f, 1f, chestRise));
        }
    }

    private void Unearth()
    {
        dug = true;
        foreach (var p in digging.ToList()) StopDigging(p);
        if (mark) Destroy(mark);
        MakeChest();
        var paid = diggers.Where(p => p && !p.IsBot).Select(p => p.Id).Distinct().ToArray();
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_DUG, diggers.Count(p => p)), "");
        foreach (var p in diggers) if (p) p.Cheer();
        Pay(paid);
        Invoke(nameof(Finish), ChestSeconds);
    }

    private async void Pay(Guid[] ids)
    {
        var game = GameManager.Instance;
        if (ids.Length == 0 || !game || game.RavenNest == null || !game.RavenNest.SessionStarted) return;
        try
        {
            var rewards = await game.RavenNest.Mangrove.ChestAsync(RavenNest.SDK.Endpoints.MangroveApi.Treasure, ids);
            MangroveRewards.Apply(game, rewards, Localization.UI_TREASURE_PAID, Localization.MSG_TREASURE_RARE);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The treasure chest could not be opened: " + exc.Message);
        }
    }

    private void Finish() => End(true);

    private void End(bool found)
    {
        if (!found) MangroveFeed.Say(Localization.UI_TREASURE_LOST, "");
        foreach (var p in digging.ToList()) StopDigging(p);
        dug = true;
        if (current == this)
        {
            current = null;
            nextMap = Time.time + Cooldown;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        foreach (var p in digging) if (p) p.Animations.EndFarming();
        if (mark) Destroy(mark);
        if (chest) Destroy(chest);
    }

    // ---- What it looks like ------------------------------------------------------------------------

    /// <summary>A red X of two planks on the ground and a mark over it; it can be clicked (StreamQuestView).</summary>
    private void MakeMark()
    {
        mark = new GameObject("Treasure X");
        mark.transform.SetParent(transform, false);
        mark.transform.position = spot + Vector3.up * 0.04f;
        mark.AddComponent<TreasureSpot>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        material.color = new Color(0.72f, 0.08f, 0.06f);
        material.SetColor("_BaseColor", new Color(0.72f, 0.08f, 0.06f));
        for (var i = 0; i < 2; ++i)
        {
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Stroke";
            Destroy(plank.GetComponent<Collider>());
            plank.transform.SetParent(mark.transform, false);
            plank.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 45f : -45f, 0f);
            plank.transform.localScale = new Vector3(0.28f, 0.05f, 2.2f);
            plank.GetComponent<Renderer>().sharedMaterial = material;
        }
        var box = mark.AddComponent<BoxCollider>();
        box.size = new Vector3(2.2f, 0.4f, 2.2f);
        WorldMark.Attach(mark, 2.2f, () => IslandNpc.Mark.Available, 1.6f);
    }

    private void MakeChest()
    {
        var prefab = Resources.Load<GameObject>("Mangrove/Treasure Chest");
        if (!prefab) return;
        chest = Instantiate(prefab, spot + Vector3.down, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), transform);
        chestRise = 0f;
    }

    // ---- The bar -----------------------------------------------------------------------------------

    public static Shinobytes.UI.MangroveView.Status CurrentStatus()
    {
        if (!current) return null;
        if (current.dug)
            return new Shinobytes.UI.MangroveView.Status { Glyph = "", Title = Localization.UI_TREASURE_BAR_DUG, Line = string.Empty };
        return new Shinobytes.UI.MangroveView.Status
        {
            Glyph = "",
            Title = string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_BAR, current.finder ? current.finder.Name : "?"),
            Line = string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_BAR_LINE, current.digging.Count, current.diggers.Count),
            Progress = current.work / DigWork,
        };
    }
}

/// <summary>The treasure's X on the ground, for StreamQuestView's clicks.</summary>
public sealed class TreasureSpot : MonoBehaviour { }

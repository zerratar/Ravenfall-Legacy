using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Silly surprises (docs/easter-eggs.md): the jokes placed in the world (<see cref="WorldGag"/>), the rare silly
/// moments (a tree says "Ow!" and its woodcutter says sorry, a fish leaps up and slaps a fisher, miners shout "Rock and
/// stone!", a pet naps, a player on Heim goes "Brrr!"), Rob Boss's paintings (<see cref="RobBossPainter"/>) and the
/// special days: Friday the 13th's black cat, "Arr!" on Talk Like a Pirate Day, and April 1st, the biggest: big heads,
/// party hats and traffic cones on enemies and pets, King Clucksworth, Rob paints only chickens, bottle notes from
/// pranksters, item names with silly words in front, and one line of fake patch notes in chat.
/// </summary>
/// <remarks>
/// Made by itself when a scene with the game in it loads, so MainWorld needs nothing. All of it follows the Silly
/// surprises setting (Settings > Game > World) and is off in potato mode and with graphics off. Nothing here touches
/// a player's state or chat: the moments are words over heads that last a few seconds. A moment comes about every
/// 12 to 25 minutes, on the island the camera watches when there is one to pick there.
/// </remarks>
public sealed class SillySurprises : MonoBehaviour
{
    public const string WorldGagsResource = "EasterEggs/World Gags";

    /// <summary>The least and the most minutes between two silly moments.</summary>
    public const float MinMinutes = 12f, MaxMinutes = 25f;

    /// <summary>How much bigger heads are on April 1st.</summary>
    public const float BigHead = 1.7f;

    public static readonly Color FindColor = new Color(1f, 0.84f, 0.32f);
    private static readonly Color WordColor = new Color(1f, 0.97f, 0.88f);
    private static readonly Color ColdColor = new Color(0.62f, 0.88f, 1f);

    public static SillySurprises Instance { get; private set; }

    /// <summary>
    /// For testing (the debug menu): null follows the computer's date, true makes every day April 1st, false makes no
    /// day April 1st. Not saved.
    /// </summary>
    public static bool? ForceAprilFools;

    /// <summary>Today, by the streamer's own clock.</summary>
    public static DateTime Today => DateTime.Now;

    /// <summary>Whether it is April 1st (or the debug menu says so) and the silly surprises are on.</summary>
    public static bool IsAprilFools => SettingsApplier.SillySurprises && (ForceAprilFools ?? (Today.Month == 4 && Today.Day == 1));

    /// <summary>For testing the special days' world scenes (a Friday the 13th): the day they go by; null is today.</summary>
    public static DateTime? TestDay;

    /// <summary>The day the world scenes go by: the test day, April 1st when forced, else today.</summary>
    private static DateTime GagDay => TestDay ?? (ForceAprilFools == true ? new DateTime(Today.Year, 4, 1) : Today);

    /// <summary>When the fake patch notes are said, on April 1st: once a session, a while after the start.</summary>
    public const float PatchNotesAfterSeconds = 10f * 60f;
    private bool patchNotesSaid;
    private float aprilSince = -1f;
    private float nextHats;
    private readonly HashSet<int> hatted = new HashSet<int>();
    private readonly List<GameObject> hats = new List<GameObject>();
    private GameObject[] hatTemplates;

    private readonly List<WorldGag> gags = new List<WorldGag>();
    private readonly Dictionary<PlayerController, Transform> heads = new Dictionary<PlayerController, Transform>();
    private readonly System.Random random = new System.Random();
    private GameObject fishTemplate;
    private float nextRefresh;
    private float nextMoment;
    private bool on;
    private bool bigHeads;

    private static Shinobytes.UI.FloatingTextView floating;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded += (scene, mode) => Ensure();
        Ensure();
    }

    private static void Ensure()
    {
        if (Instance || !FindAnyObjectByType<GameManager>()) return;
        new GameObject("Silly Surprises").AddComponent<SillySurprises>();
    }

    private void Awake()
    {
        Instance = this;
        // the first moment comes a while after the start, when players have settled into their tasks
        nextMoment = Time.time + Minutes(MinMinutes * 0.5f, MaxMinutes * 0.6f);
    }

    private void Start()
    {
        var prefab = Resources.Load<GameObject>(WorldGagsResource);
        if (!prefab) return; // a build made without the local prefab: no scenes, the moments still come
        var root = Instantiate(prefab);
        root.name = "Silly Surprises (world)";
        foreach (var gag in root.GetComponentsInChildren<WorldGag>(true))
        {
            gags.Add(gag);
            if (string.IsNullOrEmpty(gag.Island)) continue;
            var island = FindIsland(gag.Island);
            if (island) gag.transform.SetParent(island.transform, true);
        }
        var fish = root.transform.Find("Templates/Fish");
        if (fish) fishTemplate = fish.gameObject;
        var templates = new List<GameObject>();
        foreach (var name in new[] { "Templates/Party Hat", "Templates/Cone" })
        {
            var t = root.transform.Find(name);
            if (t) templates.Add(t.gameObject);
        }
        hatTemplates = templates.ToArray();
        RobBossPainter.Attach();
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var gag in gags) if (gag) Destroy(gag.gameObject);
        RemoveHats();
    }

    private static IslandController FindIsland(string name)
    {
        var game = GameManager.Instance;
        if (!game || game.Islands == null) return null;
        foreach (var island in game.Islands.All)
            if (island && string.Equals(island.Island.ToString(), name, StringComparison.OrdinalIgnoreCase)) return island;
        return null;
    }

    private float Minutes(float min, float max) => (float)(min + random.NextDouble() * (max - min)) * 60f;

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 2f;
            Refresh();
        }

        April();

        if (!on || Time.time < nextMoment) return;
        nextMoment = Time.time + (Moment(null) ? Minutes(MinMinutes, MaxMinutes) : 60f);
    }

    private void LateUpdate()
    {
        if (bigHeads) GrowHeads(BigHead);
    }

    /// <summary>The setting, the day and the scenes in the world, looked at every two seconds.</summary>
    private void Refresh()
    {
        on = SettingsApplier.SillySurprises && GraphicsToggler.GraphicsEnabled;
        var day = GagDay;
        foreach (var gag in gags) if (gag) gag.Show(on && gag.IsDay(day));

        var april = on && IsAprilFools;
        if (bigHeads && !april) GrowHeads(1f);
        if (!april && hats.Count > 0) RemoveHats();
        bigHeads = april;
    }

    // ---- April 1st ---------------------------------------------------------------------------------------------

    private void April()
    {
        if (!on || !IsAprilFools)
        {
            aprilSince = -1f;
            return;
        }
        if (aprilSince < 0f) aprilSince = Time.time;

        // one line of fake patch notes, once a session (the only April line in chat)
        if (!patchNotesSaid && Time.time - aprilSince >= PatchNotesAfterSeconds)
        {
            patchNotesSaid = true;
            var game = GameManager.Instance;
            if (game && game.RavenBot != null) game.RavenBot.Announce(Localization.MSG_APRIL_PATCH_NOTES.Replace("{version}", NextPatchVersion()));
        }

        if (Time.time >= nextHats)
        {
            nextHats = Time.time + 5f;
            PutOnHats();
        }
    }

    /// <summary>Party hats and traffic cones on the enemies near the camera and on every pet out.</summary>
    private void PutOnHats()
    {
        if (hatTemplates == null || hatTemplates.Length == 0) return;
        var cam = Camera.main;
        if (!cam) return;
        var near = cam.transform.position;
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            if (!enemy || (enemy.transform.position - near).sqrMagnitude > 90f * 90f) continue;
            Hat(enemy.transform);
        }
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        foreach (var p in game.Players.GetAllPlayers())
            if (p && p.BattlePets.Controller) Hat(p.BattlePets.Controller.transform);
    }

    private void Hat(Transform wearer)
    {
        var id = wearer.GetHashCode();
        if (!hatted.Add(id)) return;
        // the size of what is drawn (enabled renderers only: pooled and LOD ones can sit at the feet)
        var found = false;
        var bounds = new Bounds(wearer.position, Vector3.zero);
        foreach (var r in wearer.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !(r is SkinnedMeshRenderer || r is MeshRenderer) || r.bounds.size.y < 0.05f) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!found) { hatted.Remove(id); return; }
        // the head: a humanoid's head bone, or the first bone called head (animals, pets)
        var animator = wearer.GetComponentInChildren<Animator>();
        var head = animator && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (!head)
            foreach (var t in wearer.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("Head", StringComparison.OrdinalIgnoreCase) || t.name.EndsWith("_Head", StringComparison.OrdinalIgnoreCase)) { head = t; break; }
        var size = Mathf.Clamp(bounds.size.y / 1.8f, 0.25f, 4f);
        var template = hatTemplates[(id & 0x7fffffff) % hatTemplates.Length];
        var hat = Instantiate(template);
        hat.SetActive(true);
        // no head bone (the cows' bones are joint1, joint2...): the top of the front of the body, where a head is
        var fwd = Flat(wearer.forward).normalized;
        var along = Mathf.Abs(fwd.x) * bounds.extents.x + Mathf.Abs(fwd.z) * bounds.extents.z;
        var front = bounds.center + fwd * along * 0.72f;
        var top = head ? head.position + Vector3.up * 0.16f * size : new Vector3(front.x, bounds.max.y - 0.06f * size, front.z);
        hat.transform.SetPositionAndRotation(top, Quaternion.LookRotation(Flat(wearer.forward), Vector3.up) * Quaternion.Euler(0f, 0f, ((id >> 3) % 3 - 1) * 12f));
        hat.transform.localScale = template.transform.lossyScale * size;
        hat.transform.SetParent(head ? head : wearer, true);
        hats.Add(hat);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.001f ? Vector3.forward : v;
    }

    private void RemoveHats()
    {
        foreach (var hat in hats) if (hat) Destroy(hat);
        hats.Clear();
        hatted.Clear();
    }

    /// <summary>On April 1st an item's name in tooltips gets a silly word in front (the same one every time for an item).</summary>
    public static string AprilName(string name)
    {
        if (string.IsNullOrEmpty(name) || !IsAprilFools) return name;
        var words = Localization.UI_APRIL_ITEM_WORDS;
        if (words == null || words.Length == 0) return name;
        var h = 0;
        foreach (var c in name) h = h * 31 + c;
        return string.Format(Localization.UI_APRIL_ITEM_NAME, words[(h & 0x7fffffff) % words.Length], name);
    }

    /// <summary>On April 1st, a bottle's note is one of the pranksters' (null on any other day).</summary>
    public static string AprilBottleNote()
    {
        if (!IsAprilFools) return null;
        var lines = new List<string>();
        for (var n = 1; n < 30; ++n)
        {
            var field = typeof(Localization).GetField("UI_BOTTLE_APRIL_" + n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field == null) break;
            if (field.GetValue(null) is string s && !string.IsNullOrEmpty(s)) lines.Add(s);
        }
        return lines.Count == 0 ? null : lines[UnityEngine.Random.Range(0, lines.Count)];
    }

    // ---- The moments -------------------------------------------------------------------------------------------

    /// <summary>The kinds of moment, for <see cref="Trigger"/> and the docs.</summary>
    public static readonly string[] Kinds = { "ow", "rock", "slap", "nap", "brrr", "arr" };

    /// <summary>Plays a moment now (a kind from <see cref="Kinds"/>, or null for any that can happen). False when none could.</summary>
    public static bool Trigger(string kind) => Instance && Instance.on && Instance.Moment(kind);

    private bool Moment(string kind)
    {
        var order = new List<string>(Kinds);
        var now = Today;
        if (!(now.Month == 9 && now.Day == 19)) order.Remove("arr"); // Talk Like a Pirate Day only
        if (kind != null) order = order.Contains(kind) || kind == "arr" ? new List<string> { kind } : new List<string>();
        Shuffle(order);
        foreach (var k in order)
        {
            switch (k)
            {
                case "ow": if (TreeOw()) return true; break;
                case "rock": if (RockAndStone()) return true; break;
                case "slap": if (FishSlap()) return true; break;
                case "nap": if (PetNap()) return true; break;
                case "brrr": if (Brrr()) return true; break;
                case "arr": if (Arr()) return true; break;
            }
        }
        return false;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; --i)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>The players a moment can pick, those on the island the camera watches first.</summary>
    private List<PlayerController> Pick(Func<PlayerController, bool> can)
    {
        var game = GameManager.Instance;
        var all = new List<PlayerController>();
        if (!game || game.Players == null) return all;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.isDestroyed || p.InRavebridge || p.IsAway || p.Controlled) continue;
            if (p.raidHandler != null && p.raidHandler.InRaid) continue;
            if (p.dungeonHandler != null && p.dungeonHandler.InDungeon) continue;
            if (can(p)) all.Add(p);
        }
        var watched = game.Camera && game.Camera.Observer ? game.Camera.Observer.ObservedPlayer : null;
        var island = watched ? watched.Island : null;
        var here = island ? all.FindAll(p => p.Island == island) : all;
        var list = here.Count > 0 ? here : all;
        Shuffle(list);
        if (watched && list.Remove(watched) && can(watched)) list.Insert(0, watched);
        return list;
    }

    private bool TreeOw()
    {
        var woodcutters = Pick(p => p.GetTask() == TaskType.Woodcutting && p.HasTarget && !p.InCombat);
        if (woodcutters.Count == 0) return false;
        var player = woodcutters[0];
        var tree = Marker(player.TargetPosition + Vector3.up * 1.5f, 6f);
        Say(tree, Localization.UI_FLOAT_OW, WordColor, 2f);
        StartCoroutine(Later(1.4f, () => Say(player.transform, Localization.UI_FLOAT_SORRY, WordColor, 1.8f)));
        return true;
    }

    private bool RockAndStone()
    {
        var miners = Pick(p => p.GetTask() == TaskType.Mining && !p.InCombat);
        if (miners.Count == 0) return false;
        Say(miners[0].transform, Localization.UI_FLOAT_ROCK_AND_STONE, FindColor, 2f);
        // the others on the island shout it back, one after another
        var answers = 0;
        for (var i = 1; i < miners.Count && answers < 3; ++i)
        {
            if (miners[i].Island != miners[0].Island) continue;
            var miner = miners[i];
            StartCoroutine(Later(1.1f + answers * 0.6f, () => Say(miner.transform, Localization.UI_FLOAT_ROCK_AND_STONE, FindColor, 1.6f)));
            answers++;
        }
        return true;
    }

    private bool FishSlap()
    {
        if (!fishTemplate) return false;
        var fishers = Pick(p => p.GetTask() == TaskType.Fishing && p.HasTarget && !p.InCombat && !(p.ferryHandler && p.ferryHandler.Active) && p.Island);
        if (fishers.Count == 0) return false;
        StartCoroutine(Slap(fishers[0]));
        return true;
    }

    private IEnumerator Slap(PlayerController player)
    {
        var face = player.transform.position + Vector3.up * 1.7f;
        var away = player.TargetPosition - player.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = player.transform.forward;
        var start = player.transform.position + away.normalized * Mathf.Clamp(away.magnitude, 2.5f, 5f) - Vector3.up * 0.6f;
        var fish = Instantiate(fishTemplate, start, Quaternion.identity);
        fish.SetActive(true);
        const float Up = 0.75f, Down = 0.7f;
        for (var t = 0f; t < Up && player; t += Time.deltaTime)
        {
            var k = t / Up;
            face = player.transform.position + Vector3.up * 1.7f;
            var at = Vector3.Lerp(start, face, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.2f;
            fish.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face - start + Vector3.up * 0.01f) * Quaternion.Euler(0f, 0f, t * 900f));
            yield return null;
        }
        if (player) Say(player.transform, Localization.UI_FLOAT_SLAP, FindColor, 1.8f);
        var from = fish.transform.position;
        for (var t = 0f; t < Down; t += Time.deltaTime)
        {
            var k = t / Down;
            fish.transform.position = Vector3.Lerp(from, start - Vector3.up * 0.8f, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
            fish.transform.Rotate(0f, 0f, 1200f * Time.deltaTime, Space.Self);
            yield return null;
        }
        Destroy(fish);
    }

    private bool PetNap()
    {
        var owners = Pick(p => p.BattlePets.Controller && !p.BattlePets.Controller.KnockedOut && !p.InCombat);
        if (owners.Count == 0) return false;
        var pet = owners[0].BattlePets.Controller.transform;
        for (var i = 0; i < 3; ++i)
            StartCoroutine(Later(i * 2.2f, () => { if (pet) Say(pet, Localization.UI_FLOAT_ZZZ, ColdColor, 1.5f); }));
        return true;
    }

    private bool Brrr()
    {
        var cold = Pick(p => p.Island && p.Island.Island == RavenNest.Models.Island.Heim && !p.InCombat);
        if (cold.Count == 0) return false;
        Say(cold[0].transform, Localization.UI_FLOAT_BRRR, ColdColor, 1.8f);
        return true;
    }

    private bool Arr()
    {
        var anyone = Pick(p => !p.InCombat);
        if (anyone.Count == 0) return false;
        Say(anyone[0].transform, Localization.UI_FLOAT_ARR, FindColor, 1.8f);
        return true;
    }

    private static IEnumerator Later(float seconds, Action then)
    {
        yield return new WaitForSeconds(seconds);
        then();
    }

    /// <summary>An empty object for a word to float over (a tree has no object of its own), gone after a while.</summary>
    private static Transform Marker(Vector3 at, float seconds)
    {
        var marker = new GameObject("Silly Word");
        marker.transform.position = at;
        Destroy(marker, seconds);
        return marker.transform;
    }

    /// <summary>
    /// A short word over something, as the effect words are shown (bold, outlined, big enough for stream). Only with the
    /// setting on, unless <paramref name="always"/> (an odd find's "Found:", which is about a real item).
    /// </summary>
    public static void Say(Transform target, string text, Color color, float lifeScale = 1f, bool always = false)
    {
        if (!target || string.IsNullOrEmpty(text) || !GraphicsToggler.GraphicsEnabled) return;
        if (!always && !SettingsApplier.SillySurprises) return;
        if (!floating) floating = FindAnyObjectByType<Shinobytes.UI.FloatingTextView>();
        if (floating) floating.AddEffect(target, text, color, lifeScale);
    }

    // ---- April 1st: big head mode ------------------------------------------------------------------------------

    private void GrowHeads(float scale)
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) return;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p) continue;
            if (!heads.TryGetValue(p, out var head) || !head)
            {
                var animator = p.GetComponentInChildren<Animator>();
                head = animator && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                heads[p] = head;
            }
            if (head) head.localScale = Vector3.one * scale;
        }
        if (scale == 1f) heads.Clear();
    }

    /// <summary>
    ///     The fake April patch notes name the patch after the running game (0.9.7.0a gives 0.9.7.1, 0.9.8 gives
    ///     0.9.8.1), so the joke never names a version that is already old.
    /// </summary>
    private static string NextPatchVersion()
    {
        var parts = new System.Collections.Generic.List<int>();
        foreach (var p in (Ravenfall.Version ?? string.Empty).Split('.'))
        {
            var digits = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.TakeWhile(p, char.IsDigit)));
            if (digits.Length == 0) break;
            parts.Add(int.Parse(digits));
        }
        if (parts.Count == 0) return "1.0.0.1";
        if (parts.Count < 4) parts.Add(1);
        else parts[3]++;
        return string.Join(".", parts);
    }
}

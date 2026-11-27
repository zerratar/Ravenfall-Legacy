using System.Collections.Generic;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
///     Substitution in the game (docs/substitution-escape-idea.md): a rare power from the worn gear (the magical attribute
///     with the smoke step, a Shadowstone in armor with the shadow phase) that may save a player standing inside a boss's
///     telegraphed area attack as it lands. The player is somewhere else when the blow comes: a walkable spot just outside
///     the ring on their group's side, never into another ring or through a wall, and takes nothing from that blow. Never
///     against a plain swing (only the area attacks call <see cref="TryEscape"/>), so a tank holding the boss is not made
///     immune. The player keeps their target and walks back in. The chance and cooldown are RavenNest.Combat.Substitution's,
///     the best of the worn sources (EquipmentStats.Substitution). Also says the server's rare finds in chat, one line at
///     most a minute, like LegendaryNews.
/// </summary>
public static class Substitutions
{
    /// <summary>The last escape of each player (Time.time), for the cooldown.</summary>
    private static readonly Dictionary<PlayerController, float> lastEscape = new Dictionary<PlayerController, float>();

    /// <summary>Escapes this session, for tests: who, which look, where from and to, and when.</summary>
    public static readonly List<string> Log = new List<string>();

    /// <summary>For tests: every roll is a success while it is ready (the cooldown still holds). Never set in a release.</summary>
    public static bool AlwaysForTests;

    // the finds said in chat
    public const float NewsEvery = 60f;
    private static readonly List<string> pending = new List<string>();
    private static float nextLine;

    /// <summary>Lines said this session (for tests: what chat got, or would have got).</summary>
    public static readonly List<string> Said = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        lastEscape.Clear();
        Log.Clear();
        pending.Clear();
        Said.Clear();
        nextLine = 0f;
        AlwaysForTests = false;
    }

    /// <summary>A player's Substitution from their worn gear (empty for most).</summary>
    public static SubstitutionPower PowerOf(PlayerController player) =>
        player && player.EquipmentStats != null ? player.EquipmentStats.Substitution : default;

    /// <summary>Seconds until the player's Substitution is ready again (0 when ready or never used).</summary>
    public static float CooldownLeft(PlayerController player)
    {
        var power = PowerOf(player);
        if (!power.Has || !lastEscape.TryGetValue(player, out var last)) return 0f;
        return Mathf.Max(0f, (float)power.Cooldown - (Time.time - last));
    }

    /// <summary>
    ///     A boss's area attack lands at <paramref name="centre"/> (<paramref name="radius"/> wide) with the player inside
    ///     it: does Substitution save them. On an escape the player is already outside the ring, the look plays and the
    ///     word shows; the caller then leaves them out of the blow (no damage, no knockback). <paramref name="group"/> are
    ///     the others in the fight (their side is where the player lands), <paramref name="avoidCentre"/> another ring on
    ///     the ground at the same time (the landing spot stays out of it).
    /// </summary>
    public static bool TryEscape(PlayerController player, Vector3 centre, float radius, IEnumerable<PlayerController> group,
        Vector3? avoidCentre = null, float avoidRadius = 0f, CombatMeter meter = null)
    {
        if (!player || player.isDestroyed || player.Stats.IsDead) return false;
        var power = PowerOf(player);
        if (!power.Has) return false;
        var last = lastEscape.TryGetValue(player, out var t) ? t : -1f;
        var now = Time.time;
        var roll = AlwaysForTests ? 0.0 : Random.value;
        if (!Substitution.Escapes(power, last, now, roll)) return false;
        // nowhere to go (a wall all round, the edge of a ledge): no escape, and the cooldown is not spent
        if (!FindSpot(player, centre, radius, group, avoidCentre, avoidRadius, out var spot)) return false;

        lastEscape[player] = now;
        var from = player.Position;
        player.Movement.SetPosition(spot);
        // facing the fight again: they walk back in to their target
        var face = centre - spot;
        face.y = 0f;
        if (face.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(face.normalized);

        SubstitutionFx.Play(power.Look, player, from, spot);
        CombatPaths.Word(player, Localization.UI_FLOAT_SUBSTITUTION, power.Look == SubstitutionLook.ShadowPhase
            ? new Color(0.78f, 0.62f, 1f) : new Color(0.92f, 0.92f, 0.95f));
        (meter ?? CombatMeter.Tower).AddEscaped(player);
        Log.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} {1} {2} {3:0.0}m out ({4:0.0} to {5:0.0} from the middle)",
            now, player.PlayerName, power.Look, Vector3.Distance(from, spot), Flat(from - centre).magnitude, Flat(spot - centre).magnitude));
        return true;
    }

    // how far outside the ring the player lands, tried in turn
    private static readonly float[] Margins = { 1.3f, 2.1f, 3f };
    // the turns from the group's side, tried in turn (degrees)
    private static readonly float[] Turns = { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 135f, -135f, 165f, -165f };

    /// <summary>
    ///     A walkable spot just outside the ring, as near the group's side as there is one: on the navmesh within a little of
    ///     the aimed point, at about the player's height (not a ledge below or a roof above), outside any other ring, and
    ///     reachable on foot from where they stand (not through a wall into another room).
    /// </summary>
    public static bool FindSpot(PlayerController player, Vector3 centre, float radius, IEnumerable<PlayerController> group,
        Vector3? avoidCentre, float avoidRadius, out Vector3 spot)
    {
        spot = player.Position;
        var agent = player.GetComponent<NavMeshAgent>();
        var mask = agent ? agent.areaMask : NavMesh.AllAreas;

        // the group's side: where the others stand outside the ring (or all of them, or straight out from the middle)
        var outside = Vector3.zero;
        var all = Vector3.zero;
        int outsideCount = 0, allCount = 0;
        if (group != null)
        {
            foreach (var other in group)
            {
                if (!other || other == player || other.isDestroyed || other.Stats.IsDead) continue;
                var d = Flat(other.Position - centre);
                all += d;
                allCount++;
                if (d.magnitude > radius) { outside += d; outsideCount++; }
            }
        }
        var side = outsideCount > 0 ? outside / outsideCount : allCount > 0 ? all / allCount : Vector3.zero;
        // halfway between the group's side and straight out from where the player stands, so a short way out (a
        // player across the ring from the group does not fly over it)
        var own = Flat(player.Position - centre);
        var dir = (side.sqrMagnitude > 0.01f ? side.normalized : Vector3.zero) + (own.sqrMagnitude > 0.25f ? own.normalized : Vector3.zero);
        if (dir.sqrMagnitude < 0.1f) dir = side.sqrMagnitude > 0.01f ? side : own;
        if (dir.sqrMagnitude < 0.01f) dir = Flat(player.transform.forward);
        dir = dir.normalized;

        var path = new NavMeshPath();
        var maxPath = (radius * 2f + 6f) * 1.6f;
        foreach (var margin in Margins)
        {
            foreach (var turn in Turns)
            {
                var aim = centre + Quaternion.Euler(0f, turn, 0f) * dir * (radius + margin);
                aim.y = player.Position.y;
                if (!NavMesh.SamplePosition(aim, out var hit, 1.1f, mask)) continue;
                var p = hit.position;
                if (Flat(p - centre).magnitude < radius + 0.6f) continue;
                if (avoidCentre.HasValue && Flat(p - avoidCentre.Value).magnitude < avoidRadius + 0.6f) continue;
                if (Mathf.Abs(p.y - player.Position.y) > 2.5f) continue;
                if (!NavMesh.CalculatePath(player.Position, p, mask, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                if (Length(path) > maxPath) continue;
                spot = p;
                return true;
            }
        }
        return false;
    }

    private static float Length(NavMeshPath path)
    {
        var corners = path.corners;
        var length = 0f;
        for (var i = 1; i < corners.Length; ++i) length += Vector3.Distance(corners[i - 1], corners[i]);
        return length;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    // ---- the finds ----------------------------------------------------------------------------------------------

    /// <summary>
    ///     The server gave a player a rare find (GameManager.AddItems): a piece with the attribute (its enchantment) or a
    ///     Shadowstone. Said in chat once, with other finds of the same minute, and a word over the player.
    /// </summary>
    public static void Found(PlayerController player, Item item, string enchantment)
    {
        if (!player || item == null) return;
        if (Gems.IsShadowstone(item.Id))
        {
            pending.Add(string.Format(Localization.MSG_SUBSTITUTION_GEM_PART, player.PlayerName, item.Name));
        }
        else
        {
            var power = Substitution.FromEnchantment(enchantment);
            if (!power.Has) return;
            pending.Add(string.Format(Localization.MSG_SUBSTITUTION_ITEM_PART, player.PlayerName, item.Name,
                Mathf.RoundToInt((float)power.Chance * 100) + "%"));
        }
        CombatPaths.Word(player, Localization.UI_FLOAT_SUBSTITUTION_FOUND, new Color(0.78f, 0.62f, 1f));
    }

    /// <summary>Is this reward a Substitution find (GameManager.AddItems).</summary>
    public static bool IsFind(EventItemReward reward) =>
        reward != null && (Gems.IsShadowstone(reward.ItemId) || Substitution.HasAttribute(reward.Enchantment));

    /// <summary>Once a frame (GameManager): the finds' chat line, at most once a minute.</summary>
    public static void Update()
    {
        if (pending.Count == 0 || Time.unscaledTime < nextLine) return;
        var game = GameManager.Instance;
        if (!game) return;
        var line = string.Join("; ", pending);
        pending.Clear();
        nextLine = Time.unscaledTime + NewsEvery;
        Said.Add(line);
        game.RavenBot?.Announce(Localization.MSG_SUBSTITUTION, line);
    }
}

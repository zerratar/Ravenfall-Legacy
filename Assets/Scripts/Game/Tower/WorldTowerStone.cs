using RavenNest.Combat;
using UnityEngine;

/// <summary>
///     The best climbers' stone at the tower's foot, the tower stele the owner picked (design C,
///     docs/mockups/tower/world/stone-c-tower-stele.jpg; docs/world-tower.md): the left third of its face is the tower
///     carved as a stack of tiers, one per floor from the foot up, lit gold for the floors the stream has reached (the
///     floor it is on breathes, as its section of the tower does) and dark above. The right two thirds carry the words
///     (<see cref="WorldTower.SlabPlace"/>, the climbers' rows in WorldTowerClimbers.cs).
///     <para>The tiers are a fixed number of carved shapes (tools/tower/world/world_tower.py, STELE_TIERS), each in a
///     lit and a dark look; which floor a tier stands for is worked out here, so the tower can have any number of
///     floors: while it has no more floors than there are tiers, tier n is floor n (tiers over the last floor stay
///     dark, the tower goes on); with more floors the tiers show a window of them round the stream's floor (two below
///     it, the rest above), and each tier's window carries its floor's number so the window is read right.</para>
/// </summary>
public sealed partial class WorldTower
{
    [Header("The stone's carved tower")]
    [Tooltip("The carved tiers, from the foot up: the lit look (gold) of each.")]
    public Renderer[] StoneTierLit = new Renderer[0];
    [Tooltip("The dark look of each tier.")]
    public GameObject[] StoneTierDark = new GameObject[0];
    [Tooltip("Each tier's floor number, shown only while the tiers show a window of a taller tower.")]
    public TMPro.TextMeshPro[] StoneTierNumbers = new TMPro.TextMeshPro[0];

    /// <summary>How many tiers under the stream's floor a window shows.</summary>
    private const int TiersBelow = 2;

    private Material stoneLit, stoneCurrent;

    /// <summary>
    ///     The floor each of <paramref name="tiers"/> carved tiers stands for, from the foot up, for a tower of
    ///     <paramref name="floors"/> floors with the stream on <paramref name="current"/> (0 when it is on none: all
    ///     cleared, or nothing known yet). A tier over the tower's last floor stands for the floor it would be.
    ///     Returns whether the tiers show a window of a taller tower (their numbers are shown then).
    /// </summary>
    public static bool StoneTierFloors(int tiers, int floors, int current, int cleared, int[] into)
    {
        var first = 1;
        var window = floors > tiers;
        if (window)
        {
            // the stream's floor two tiers up; with none (all cleared) the top of the tower
            var at = current > 0 ? current : (cleared >= floors ? floors : 1);
            first = Mathf.Clamp(at - TiersBelow, 1, floors - tiers + 1);
        }
        for (var i = 0; i < tiers && i < into.Length; ++i) into[i] = first + i;
        return window;
    }

    private int[] tierFloors;

    /// <summary>Lights the carved tiers by the stream's progress (on every change of the stream's step).</summary>
    private void StoneTiers()
    {
        var n = Mathf.Min(StoneTierLit.Length, StoneTierDark.Length);
        if (n == 0) return;
        if (!stoneLit && Shared)
        {
            // their own copies of the tower's material: lit steadily, and breathing for the stream's floor (the
            // breathing stops in potato mode with the rest of the tower's moving light, _WorldTowerAnimate)
            stoneLit = new Material(Shared) { name = Shared.name + " (stone, lit)", hideFlags = HideFlags.DontSave };
            stoneLit.SetFloat(GlowId, 1f);
            stoneLit.SetFloat(PulseId, 0f);
            stoneCurrent = new Material(Shared) { name = Shared.name + " (stone, the stream's floor)", hideFlags = HideFlags.DontSave };
            stoneCurrent.SetFloat(GlowId, 1f);
            stoneCurrent.SetFloat(PulseId, Pulse);
        }
        if (tierFloors == null || tierFloors.Length != n) tierFloors = new int[n];
        var current = CurrentFloor;
        var window = StoneTierFloors(n, Tower.FloorCount, current, cleared, tierFloors);
        for (var i = 0; i < n; ++i)
        {
            var floor = tierFloors[i];
            // nothing known yet (step 0): all dark
            var lit = step > 0 && (floor <= cleared || floor == current);
            if (StoneTierLit[i])
            {
                StoneTierLit[i].gameObject.SetActive(lit);
                if (lit && stoneLit) StoneTierLit[i].sharedMaterial = floor == current ? stoneCurrent : stoneLit;
            }
            if (StoneTierDark[i]) StoneTierDark[i].SetActive(!lit);
            if (i < StoneTierNumbers.Length && StoneTierNumbers[i])
                StoneTierNumbers[i].text = window && floor <= Tower.FloorCount ? floor.ToString() : "";
        }
    }

    private void OnDestroyStone()
    {
        if (stoneLit) Destroy(stoneLit);
        if (stoneCurrent) Destroy(stoneCurrent);
    }

    /// <summary>The row of the stone for a climber: the place and the name on the left, the clears at the right end.</summary>
    private static string StoneRow(int place, string name, int clears) =>
        // two lines laid over each other (the second has no height of its own): one aligned left, one right
        "<align=left><margin-right=2.4em>" + string.Format(Localization.UI_TOWER_STONE_ROW, place, name) + "</margin><line-height=0>\n<align=right>" + clears;
}

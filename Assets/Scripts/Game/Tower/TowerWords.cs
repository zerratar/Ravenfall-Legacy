using RavenNest.Combat;

/// <summary>
///     The tower in the players' words (docs/tower-worlds-proposal.md section 12): the tower has floors, a floor has
///     parts, and a part's guardian is a mini boss or, on the floor's last part, the floor boss. The game and the server
///     count in steps (a part's number through every floor); this says a step the way it is shown and typed.
/// </summary>
public static class TowerWords
{
    /// <summary>The floor a step is on, and which part of it (for a step past the top: the top's).</summary>
    public static int Floor(int step) => Tower.FloorOf(Clamp(step));
    public static int Part(int step) => Tower.PartOf(Clamp(step));

    private static int Clamp(int step) => step < 1 ? 1 : step > Tower.TopFloor ? Tower.TopFloor : step;

    /// <summary>"Floor 2, part 4".</summary>
    public static string Place(int step) => string.Format(Localization.UI_TOWER_PLACE, Floor(step), Part(step));

    /// <summary>The name of the floor a step is on: "The Frozen Vault".</summary>
    public static string FloorName(int step) => Tower.BandOf(Clamp(step))?.Name ?? "";

    /// <summary>"mini boss" or "floor boss": what the guardian of a step is.</summary>
    public static string BossKind(int step) => Tower.IsFloorBoss(step) ? Localization.UI_TOWER_FLOOR_BOSS : Localization.UI_TOWER_MINI_BOSS;
}

using System.Globalization;

/// <summary>
/// Decor that something other than the build mode tray unlocks (docs/village-decor-design.md, phase 4): hero mode's
/// waves 10, 25 and 50, the regatta, the pirates' raid and Ravebridge's quests each give the village a piece on the
/// server (VillageDecorManager.UnlockForQuest), and the patron pieces wait for the streamer's Patreon tier. This says
/// so in chat, has the Decor tab look again, and words what a locked piece waits for.
/// </summary>
public static class VillageDecorUnlocks
{
    /// <summary>The server unlocked a piece for the village: chat hears it, and the Decor tab shows it unlocked.</summary>
    public static void Announce(GameManager game, string key)
    {
        if (!game || string.IsNullOrEmpty(key)) return;
        var decor = game.Village ? game.Village.Decor : null;
        var name = decor ? decor.NameOf(key) : Localization.DecorName(key, key);
        game.RavenBot?.Announce(Localization.MSG_VILLAGE_DECOR_UNLOCKED, Reason(key), name);
        if (decor) _ = decor.RefreshPiecesAsync();
    }

    private static string Reason(string key)
    {
        switch (key)
        {
            case "hero_banner_01":
            case "goblin_drum_01":
            case "statue_hero_ravebridge_01": return Localization.MSG_DECOR_REASON_HERO;
            case "regatta_trophy_01": return Localization.MSG_DECOR_REASON_REGATTA;
            case "pirate_cannon_01": return Localization.MSG_DECOR_REASON_PIRATES;
            case "ravebridge_lion_01": return Localization.MSG_DECOR_REASON_RAVEBRIDGE;
            default: return string.Empty;
        }
    }

    /// <summary>What a piece with a quest key waits for: a chain, a village story, or one of the newer unlocks.</summary>
    public static string QuestHint(string quest)
    {
        switch (quest)
        {
            case "hero10": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_HERO, 10);
            case "hero25": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_HERO, 25);
            case "hero50": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_HERO, 50);
            case "regatta": return Localization.UI_DECOR_LOCKED_REGATTA;
            case "pirates": return Localization.UI_DECOR_LOCKED_PIRATES;
            case "ravebridge": return Localization.UI_DECOR_LOCKED_RAVEBRIDGE;
            // the streamer's own stories are begun in the quest window, not with !quest begin
            case "runestone": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_STORY, Localization.UI_STORY_RUNESTONE);
            case "bananas": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_STORY, Localization.UI_STORY_BANANAS);
            case "roadwest": return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_STORY, Localization.UI_STORY_ROADWEST);
            default: return string.Format(CultureInfo.InvariantCulture, Localization.UI_DECOR_LOCKED_QUEST, QuestChat.ChainName(quest), quest);
        }
    }

    private static readonly string[] Tiers = { "", "Mithril", "Rune", "Dragon", "Abraxas", "Phantom" };

    /// <summary>A Patreon tier's name (the tiers are names, the same in every language).</summary>
    public static string PatronTier(int tier) => Tiers[UnityEngine.Mathf.Clamp(tier, 0, Tiers.Length - 1)];
}

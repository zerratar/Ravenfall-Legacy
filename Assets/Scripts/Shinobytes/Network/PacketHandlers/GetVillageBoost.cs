using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GetVillageBoost : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public GetVillageBoost(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var experience = Game.Village.TownHall.Experience;
        var level = Game.Village.TownHall.Level;
        var nextLevel = level + 1;
        var nextLevelExperience = GameMath.ExperienceForLevel(nextLevel);
        var remainingExp = nextLevelExperience - experience;

        var bonuses = Game.Village.GetExpBonuses();
        GetBonusMessage(bonuses, out var bonusString, out var bonusValue);
        client.SendFormat(data.Username, bonusString, Game.Village.TownHall.Level, remainingExp, bonusValue);
    }

    private void GetBonusMessage(ICollection<TownHouseExpBonus> bonuses, out string format, out string bonus)
    {
        // Village is level 100, active boosts TYPE VAL%, TYPE        

        bonus = string.Join(", ", bonuses.Where(x => x.Bonus > 0).GroupBy(x => x.SlotType)
            .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
            .Select(x => $"{x.Key} {x.Sum(y => y.Bonus)}%"));

        if (!string.IsNullOrEmpty(bonus))
        {
            format = Localization.MSG_VILLAGE_BOOST;
        }
        else
        {
            format = Localization.MSG_VILLAGE_BOOST_NO_BOOST;
        }
    }
}

public class SetVillageHuts : ChatBotCommandHandler<PlayerStringRequest>
{
    public SetVillageHuts(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerStringRequest data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data.Player);
        if (!player || (!player.IsBroadcaster && !player.IsGameAdmin && !player.IsGameModerator))
        {
            Shinobytes.Debug.LogWarning(data.Player?.Username + " tried to set the village boost but does not have permission to do so.");
            return;
        }

        if (string.IsNullOrEmpty(data.Value))
        {
            Shinobytes.Debug.LogWarning(data.Player?.Username + " tried to set the village boost forgot to enter a target skill.");
            return;
        }

        var str = data.Value.ToLower().Trim();
        var result = TownHouseSlotType.Empty;

        if (str.StartsWith("str") || str.StartsWith("def") || str.StartsWith("health") || str.StartsWith("atk") || str.StartsWith("att"))
        {
            result = TownHouseSlotType.Melee;
        }
        else if (!System.Enum.TryParse<TownHouseSlotType>(str, true, out result))
        {
            // meh, we will be a little bit harsh here. They have to spell it correctly
            client.SendFormat(data.Player.Username, "{targetName} is not a valid skill name for the huts. Make sure you use the full and proper names. Ex. Healing and not heal.", str);
            return;
        }

        client.SendFormat(data.Player.Username, "Updating village to target {targetName}. Please wait..", str);

        await Game.Village.SetVillageBoostTarget(result);

        var bonuses = Game.Village.GetExpBonuses();
        GetBonusMessage(bonuses, out var bonusString, out var bonusValue);
        client.SendFormat("", bonusString, str, bonusValue);
    }

    private void GetBonusMessage(ICollection<TownHouseExpBonus> bonuses, out string format, out string bonus)
    {
        // Village is level 100, active boosts TYPE VAL%, TYPE        
        format = Localization.MSG_VILLAGE_UPDATED;
        bonus = string.Join(", ", bonuses.Where(x => x.Bonus > 0).GroupBy(x => x.SlotType)
            .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
            .Select(x => $"{x.Key} {x.Sum(y => y.Bonus)}%"));
    }
}
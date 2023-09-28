using System.Collections.Generic;
using System.Linq;

public class SetVillageHuts : ChatBotCommandHandler<string>
{
    public SetVillageHuts(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var user = gm.Sender;
        if (!user.IsModerator && !user.IsBroadcaster)
        {
            var player = PlayerManager.GetPlayer(gm.Sender);
            if (!player)
            {
                return;
            }
            if (!player.IsGameAdmin && !player.IsGameModerator)
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(data))
        {
            Shinobytes.Debug.LogWarning(gm.Sender.Username + " tried to set the village boost forgot to enter a target skill.");
            return;
        }

        var str = data.ToLower().Trim();
        var result = TownHouseSlotType.Empty;

        if (str.StartsWith("str") || str.StartsWith("def") || str.StartsWith("health") || str.StartsWith("atk") || str.StartsWith("att"))
        {
            result = TownHouseSlotType.Melee;
        }
        else if (!System.Enum.TryParse<TownHouseSlotType>(str, true, out result))
        {
            // meh, we will be a little bit harsh here. They have to spell it correctly
            client.SendReply(gm, "{targetName} is not a valid skill name for the huts. Make sure you use the full and proper names. Ex. Healing and not heal.", str);
            return;
        }

        client.SendReply(gm, "Updating village to target {targetName}. Please wait..", str);

        await Game.Village.SetVillageBoostTarget(result);

        var bonuses = Game.Village.GetExpBonuses();
        GetBonusMessage(bonuses, out var formatString, out var bonusValue);
        client.SendReply(gm, formatString, str, bonusValue);
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
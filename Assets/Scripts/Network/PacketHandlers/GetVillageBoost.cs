using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GetVillageBoost : PacketHandler<Player>
{
    public GetVillageBoost(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var bonuses = Game.Village.GetExpBonuses();
        GetBonusMessage(bonuses, out var bonusString, out var bonusValue);
        client.SendFormat(data.Username, bonusString, Game.Village.TownHall.Level, bonusValue);
    }

    private void GetBonusMessage(ICollection<TownHouseExpBonus> bonuses, out string format, out string bonus)
    {
        // Village is level 100, active boosts TYPE VAL%, TYPE        
        format = Localization.MSG_VILLAGE_BOOST;
        bonus = string.Join(", ", bonuses.GroupBy(x => x.SlotType)
            .Where(x => x.Key != TownHouseSlotType.Empty && x.Key != TownHouseSlotType.Undefined)
            .Select(x => $"{x.Key} {x.Sum(y => y.Bonus)}%"));
    }
}

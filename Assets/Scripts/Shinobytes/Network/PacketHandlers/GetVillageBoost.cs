using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GetVillageBoost : ChatBotCommandHandler<User>
{
    public GetVillageBoost(
         GameManager game,
         RavenBotConnection server,
         PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(User data, GameMessage gm, GameClient client)
    {
        var experience = Game.Village.TownHall.Experience;
        var level = Game.Village.TownHall.Level;
        var nextLevel = level + 1;
        var nextLevelExperience = GameMath.ExperienceForLevel(nextLevel);
        var remainingExp = nextLevelExperience - experience;

        var bonuses = Game.Village.GetExpBonuses();
        GetBonusMessage(bonuses, out var bonusString, out var bonusValue);
        client.SendReply(gm, bonusString, Game.Village.TownHall.Level, remainingExp, bonusValue);
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

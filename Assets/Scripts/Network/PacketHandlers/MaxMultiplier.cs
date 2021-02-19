using System;
using System.Linq;
using UnityEngine;

public class MaxMultiplier : PacketHandler<Player>
{
    public MaxMultiplier(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
  : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var hutMulti = 1f;
        var tierSub = player.IsSubscriber ? (decimal)TwitchEventManager.TierExpMultis[Game.Permissions.SubscriberTier] : 1m;
        var multi = (float)tierSub;

        if (Game.Boost.Active)
            multi += Game.Boost.Multiplier;

        if (player.Ferry.OnFerry)
        {
            var skillIndex = player.GetSkillTypeFromArgs("sail");
            if (skillIndex != -1)
            {
                hutMulti = Game.Village.GetExpBonusBySkill((Skill)skillIndex);
                multi += hutMulti;
            }
        }
        else
        {
            var taskArgs = player.GetTaskArguments();
            var combatType = PlayerController.GetCombatTypeFromArg(taskArgs.FirstOrDefault());
            if (combatType != -1)
            {
                hutMulti = Game.Village.GetExpBonusBySkill((CombatSkill)combatType);
                multi += hutMulti;
            }
            else
            {
                var skillIndex = player.GetSkillTypeFromArgs(taskArgs);
                if (skillIndex != -1)
                {
                    hutMulti = Game.Village.GetExpBonusBySkill((Skill)skillIndex);
                    multi += hutMulti;
                }
            }
        }
        client.SendFormat(data.Username, "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub and {hutMulti}x from huts.",
            multi, tierSub, hutMulti);
    }
}

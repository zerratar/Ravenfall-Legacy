using UnityEngine;
using Skill = RavenNest.Models.Skill;
public class MaxMultiplier : ChatBotCommandHandler
{
    public MaxMultiplier(
      GameManager game,
      RavenBotConnection server,
      PlayerManager playerManager)
  : base(game, server, playerManager)
    {
    }

    public override async void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        await ExpMultiplierChecker.RunAsync(Game);

        var hutMulti = 0f;
        var tierSub = player.GetTierExpMultiplier();
        var multi = (float)tierSub;
        var rested = false;

        // Prepare global boost info if active
        string globalBoostInfo = "";
        object[] globalArgs = new object[0];
        if (Game.Boost.Active)
        {
            globalBoostInfo = Localization.MSG_EXPBOOST_GLOBAL_PART;
            globalArgs = new object[] { Game.Boost.Multiplier, Utility.FormatTime(Game.Boost.TimeLeft) };
            multi += Game.Boost.Multiplier;
        }

        if (player.ferryHandler.OnFerry)
        {
            hutMulti = Game.Village.GetExpBonusBySkill(Skill.Sailing);
            multi += hutMulti;
        }
        else if (player.ActiveSkill != Skill.None)
        {
            hutMulti = Game.Village.GetExpBonusBySkill(player.ActiveSkill);
            multi += hutMulti;
        }

        if (player.raidHandler.InRaid || player.dungeonHandler.InDungeon)
        {
            var slayerBonus = Game.Village.GetExpBonusBySkill(Skill.Slayer);
            hutMulti += slayerBonus;
            multi += slayerBonus;
        }
        multi = System.Math.Max(1, multi);
        if (player.Rested.ExpBoost > 1 && player.Rested.RestedTime > 0)
        {
            var rexp = (float)player.Rested.ExpBoost;
            multi = Mathf.Max(rexp * multi, rexp);
            rested = true;
        }

        // Sends the reply with the global boost info appended, and the rested part after it when asked for.
        // The parts are joined after translation, so the args follow the order of the parts.
        void Reply(string format, bool restedPart, params object[] args)
        {
            var allArgs = new System.Collections.Generic.List<object>(args);
            allArgs.AddRange(globalArgs);
            format += globalBoostInfo;
            if (restedPart)
            {
                format += Localization.MSG_EXPBOOST_RESTED_PART;
                allArgs.Add((float)player.Rested.ExpBoost);
            }
            client.SendReply(gm, format, allArgs.ToArray());
        }

        if (rested)
        {
            if (hutMulti > 0)
            {
                if (tierSub > 0)
                {
                    Reply(Localization.MSG_EXPBOOST_TIER_HUTS_LIST, true, multi, tierSub, hutMulti);
                }
                else
                {
                    Reply(Localization.MSG_EXPBOOST_HUTS, true, multi, hutMulti);
                }
                return;
            }

            if (tierSub > 0)
            {
                Reply(Localization.MSG_EXPBOOST_TIER, true, multi, tierSub);
            }
            else
            {
                Reply(Localization.MSG_EXPBOOST_RESTED_ONLY, false, multi);
            }
            return;
        }

        if (hutMulti > 0)
        {
            if (tierSub > 0)
            {
                Reply(Localization.MSG_EXPBOOST_TIER_AND_HUTS, false, multi, tierSub, hutMulti);
            }
            else
            {
                Reply(Localization.MSG_EXPBOOST_HUTS, false, multi, hutMulti);
            }
            return;
        }

        if (tierSub > 0)
        {
            Reply(Localization.MSG_EXPBOOST_TIER, false, multi, tierSub);
        }
        else
        {
            Reply(Localization.MSG_EXPBOOST, false, multi);
        }
    }
}

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

        if (Game.Boost.Active)
            multi += Game.Boost.Multiplier;


        if (player.ferryHandler.OnFerry)
        {
            hutMulti = Game.Village.GetExpBonusBySkill(Skill.Sailing);
            multi += hutMulti;
        }
        else
        {
            if (player.ActiveSkill != Skill.None)
            {
                hutMulti = Game.Village.GetExpBonusBySkill(player.ActiveSkill);
                multi += hutMulti;

                //var taskArgs = player.GetTaskArguments();
                //var combatType = PlayerController.GetCombatTypeFromArg(taskArgs.FirstOrDefault());
                //if (combatType != -1)
                //{
                //    hutMulti = Game.Village.GetExpBonusBySkill((CombatSkill)combatType);
                //    multi += hutMulti;
                //}
                //else
                //{
                //    var skillIndex = player.GetSkillTypeFromArgs(taskArgs);
                //    if (skillIndex != -1)
                //    {
                //        hutMulti = Game.Village.GetExpBonusBySkill((TaskSkill)skillIndex);
                //        multi += hutMulti;
                //    }
                //}
            }
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

        if (rested)
        {
            if (hutMulti > 0)
            {
                if (tierSub > 0)
                {
                    client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon, {hutMulti}x from huts and the total is multiplied by {restedMulti}x from being rested.", multi, tierSub, hutMulti, (float)player.Rested.ExpBoost);
                }
                else
                {
                    client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {hutMulti}x from huts and the total is multiplied by {restedMulti}x from being rested.", multi, hutMulti, (float)player.Rested.ExpBoost);
                }

                return;
            }

            if (tierSub > 0)
            {
                client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon and the total is multiplied by {restedMulti}x from being rested.", multi, tierSub, (float)player.Rested.ExpBoost);
            }
            else
            {
                client.SendReply(gm, "You're gaining {expMulti}x more exp for being rested.", multi);
            }
            return;
        }

        if (hutMulti > 0)
        {
            if (tierSub > 0)
            {
                client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon and {hutMulti}x from huts.", multi, tierSub, hutMulti);
            }
            else
            {
                client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {hutMulti}x from huts.", multi, hutMulti);
            }
            return;
        }

        if (tierSub > 0)
        {
            client.SendReply(gm, "Your current exp boost: {expMulti}x. You gain {tierMulti}x from sub/patreon.", multi, tierSub);
        }
        else
        {
            client.SendReply(gm, "Your current exp boost: {expMulti}x.", multi);
        }
    }
}

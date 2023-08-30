using Skill = RavenNest.Models.Skill;
public class TrainingInfo : ChatBotCommandHandler
{
    public TrainingInfo(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        //var taskType = player.GetTask();
        //if (taskType != TaskType.None)

        var skill = player.GetActiveSkillStat();
        if (skill != null)
        {
            var skillName = skill.Name;
            if (skill.Type == Skill.Health)
                skillName = "All";

            if (!string.IsNullOrEmpty(skillName))
            {
                client.SendReply(gm, Localization.MSG_TRAINING, skillName);
                return;
            }
        }

        client.SendReply(gm, Localization.MSG_TRAINING_NOTHING);
    }
}

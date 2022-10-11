public class TrainingInfo : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public TrainingInfo(
       GameManager game,
       RavenBotConnection server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendMessage(data.Username, Localization.MSG_NOT_PLAYING);
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
                client.SendMessage(data.Username, Localization.MSG_TRAINING, skillName);
                return;
            }
        }

        client.SendMessage(data.Username, Localization.MSG_TRAINING_NOTHING);
    }
}

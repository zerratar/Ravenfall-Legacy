public class TrainingInfo : PacketHandler<Player>
{
    public TrainingInfo(
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

        var taskType = player.GetTask();
        if (taskType != TaskType.None)
        {
            string skill = "";
            if (taskType == TaskType.Fighting)
            {
                var args = player.GetTaskArguments();
                var skillIndex = player.GetCombatTypeFromArgs(args);
                if (skillIndex == 3)
                {
                    skill = "all";
                }
                else if (skillIndex >= 0)
                {
                    skill = player.GetCombatSkill(skillIndex)?.Name;
                }
            }
            else
            {
                skill = taskType.ToString();
            }

            if (!string.IsNullOrEmpty(skill))
            {
                client.SendMessage(data.Username, Localization.MSG_TRAINING, skill);
                return;
            }
        }

        client.SendMessage(data.Username, Localization.MSG_TRAINING_NOTHING);
    }
}

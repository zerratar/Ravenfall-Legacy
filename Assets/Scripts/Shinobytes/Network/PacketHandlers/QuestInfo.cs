/// <summary>
/// !quest from bots that send it without what came after (quest_info): the quest list, as !quest alone. Newer bots
/// send quest_command with the arguments (QuestCommand).
/// </summary>
public class QuestInfo : ChatBotCommandHandler
{
    public QuestInfo(
     GameManager game,
     RavenBotConnection server,
     PlayerManager playerManager)
     : base(game, server, playerManager)
    {
    }

    public override void Handle(GameMessage gm, GameClient client)
    {
        _ = QuestChat.HandleAsync(Game, PlayerManager, string.Empty, gm, client);
    }
}

/// <summary>
/// !quest: there are no quests yet, so the reply says as much, and in the tavern the quest giver (or the barkeep when
/// there is none) says it to the player too. A teaser for the quests that come in a later update.
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
        client.SendReply(gm, Localization.MSG_QUEST_NONE);

        if (!Game.Tavern || !Game.Tavern.IsActivated)
        {
            return;
        }

        var npc = TavernNpc.Find(TavernNpc.NpcRole.QuestGiver)
            ?? TavernNpc.Find(TavernNpc.NpcRole.Barkeep)
            ?? TavernNpc.Find(TavernNpc.NpcRole.GhostBarkeep);
        if (!npc)
        {
            return;
        }

        var player = PlayerManager.GetPlayer(gm.Sender);
        var name = player ? player.PlayerName : gm.Sender?.Username;
        npc.Say(string.Format(Localization.UI_TAVERN_QUEST_REPLY, name));
    }
}

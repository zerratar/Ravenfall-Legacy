using RavenNest.Models;

public class Gather : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;
    public Gather(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
        var ioc = game.gameObject.GetComponent<IoCContainer>();
        this.itemResolver = ioc.Resolve<IItemResolver>();
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        if (!TryGetPlayer(gm, client, out var player))
        {
            return;
        }

        var query = (data ?? "").Trim().ToLower();
        if (string.IsNullOrEmpty(query))
        {
            player.SetTask(TaskType.Gathering);
            return;
        }

        var result = itemResolver.Resolve(query, ItemType.Gathering);
        if (result.SuggestedItemNames != null && result.SuggestedItemNames.Length > 0)
        {
            var message = Utility.ReplaceLastOccurrence(string.Join(", ", result.SuggestedItemNames), ", ", " or ");
            client.SendReply(gm, Localization.MSG_GATHERING_SUGGEST, query, message);
            return;
        }

        if (result.Item == null)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND, query);
            return;
        }

        // when using !fish and have an argument, we have to validate the item
        // if the item does not exist, let them know
        // if the item can not be farmed (not of correct type) let them know
        // if the item requires higher level of skill, let them know
        // if everything is ok, then we can start gathering the item
    }
}

using RavenNest.Models;
public class Fish : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;
    public Fish(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var taskType = TaskType.Fishing;
        var itemType = ItemType.Fishing;
        var playerSkill = player.Stats.Fishing.MaxLevel;
        var query = (data ?? "").Trim().ToLower();
        if (string.IsNullOrEmpty(query))
        {
            player.SetTask(taskType);
            return;
        }

        var result = itemResolver.Resolve(query, x => x.Type == itemType && Game.Items.CanBeDropped(x));
        if (result.SuggestedItemNames != null && result.SuggestedItemNames.Length > 0)
        {
            var message = Utility.ReplaceLastOccurrence(string.Join(", ", result.SuggestedItemNames), ", ", " or ");
            client.SendReply(gm, Localization.MSG_FISH_SUGGEST, query, message);
            return;
        }

        if (result.Item == null)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND, query);
            return;
        }

        int levelRequirement = Game.Items.GetRequiredLevelForDrop(result.Item);
        if (playerSkill < levelRequirement)
        {
            client.SendReply(gm, Localization.MSG_FISH_LEVEL_REQUIREMENT, levelRequirement, result.Item.Name);
        }

        player.SetTask(taskType, result.Item.Name);
    }
}

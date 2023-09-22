using System.Linq;
using RavenNest.Models;

public class Mine : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;

    public Mine(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        var taskType = TaskType.Mining;
        var itemType = ItemType.Mining;
        var playerSkill = player.Stats.Mining.MaxLevel;

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
            client.SendReply(gm, Localization.MSG_MINE_SUGGEST, query, message);
            return;
        }

        if (result.Item == null)
        {
            client.SendReply(gm, Localization.MSG_BUY_ITEM_NOT_FOUND, query);
            return;
        }

        // check if the target item can be dropped with player's current skill level
        // if no, then tell them that they are not high enough level to mine that item

        int levelRequirement = Game.Items.GetRequiredLevelForDrop(result.Item);

        //if (player.Island)
        //{
        //    var chunks = Game.Chunks.GetChunksOfType(player, taskType);
        //    if (chunks.Count > 0 && chunks.All(x => x.GetRequiredSkillLevel() < levelRequirement))
        //    {
        //        canMineTargetHere = false;
        //        client.SendReply(gm, "You can't mine {oreName} on this island", result.Item.Name, levelRequirement);
        //    }
        //}

        if (playerSkill < levelRequirement)
        {
            client.SendReply(gm, Localization.MSG_MINE_LEVEL_REQUIREMENT, levelRequirement, result.Item.Name);
        }

        player.SetTask(taskType, result.Item.Name);
    }
}

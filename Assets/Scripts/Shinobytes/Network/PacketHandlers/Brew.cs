﻿public class Brew : ChatBotCommandHandler<string>
{
    private IItemResolver itemResolver;
    public Brew(GameManager game, RavenBotConnection server, PlayerManager playerManager)
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

        // check so that the player is currently training cooking and at a cooking station
        // then check if the item or recipe exists.
        // if the player does not have all the required ingredients or required cooking level, let them know
        // if all is ok, then cook the item
    }
}
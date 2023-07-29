﻿public class Fish : ChatBotCommandHandler<string>
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

        // when using !fish and have an argument, we have to validate the item
        // if the item does not exist, let them know
        // if the item can not be farmed (not of correct type) let them know
        // if the item requires higher level of skill, let them know
        // if everything is ok, then we can start gathering the item
    }
}

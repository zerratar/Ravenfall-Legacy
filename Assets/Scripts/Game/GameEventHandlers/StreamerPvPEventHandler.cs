public class StreamerPvPEventHandler : GameEventHandler<StreamRaidInfo>
{
    public override void Handle(GameManager gameManager, StreamRaidInfo data)
    {
#warning STREAMER PVP !! Implement both server and client side
        // ** Two types of Streamer PvP ? one where only the "starter" fights, and one where both streams have the same fight.
        //     one could be for just "simulation" and other is for actual fight.
        //    Client that starts the fight generates a seed to use and share state with other client through the server.

        // TODO: 1. share random seed between both clients that will fight eachother
        //       2. make sure all player positions are the same or similar (move players to war island on both streams)
        // ...   ?.
        //       4. profit
    }
}

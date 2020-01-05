using UnityEngine;

public class CommandServer : MonoBehaviour
{
    private GameServer server;

    public GameServer Server => server;

    public void StartServer(GameManager gameManager)
    {
        server = new GameServer(gameManager);

        server.Register<KickPlayer>("kick");
        server.Register<PlayerLeave>("leave");

        server.Register<IslandInfo>("island_info");
        server.Register<TrainingInfo>("train_info");

        //server.Register<BanPlayer>("ban");

        server.Register<AppearanceChange>("change_appearance");

        server.Register<PlayerJoin>("join");
        server.Register<PlayerTask>("task");

        server.Register<ObservePlayer>("observe");

        server.Register<ToggleHelmetVisibility>("toggle_helmet");
        server.Register<CycleEquippedPet>("toggle_pet");

        server.Register<BuyItem>("buy_item");
        server.Register<SellItem>("sell_item");

        server.Register<VendorItem>("vendor_item");
        server.Register<GiftItem>("gift_item");
        server.Register<ValueItem>("value_item");
        server.Register<CraftRequirement>("req_item");

        server.Register<PlayerStats>("player_stats");
        server.Register<PlayerResources>("player_resources");
        server.Register<HighestSkill>("highest_skill");

        server.Register<TwitchSubscriber>("twitch_sub");
        server.Register<TwitchCheerer>("twitch_cheer");

        server.Register<ArenaJoin>("arena_join");
        server.Register<ArenaLeave>("arena_leave");

        server.Register<ArenaKick>("arena_kick");
        server.Register<ArenaAdd>("arena_add");
        server.Register<ArenaBegin>("arena_begin");
        server.Register<ArenaEnd>("arena_end");

        server.Register<DuelPlayer>("duel");
        server.Register<DuelCancel>("duel_cancel");
        server.Register<DuelAccept>("duel_accept");
        server.Register<DuelDecline>("duel_decline");

        server.Register<RaidJoin>("raid_join");
        server.Register<RaidForce>("raid_force");

        server.Register<DungeonJoin>("dungeon_join");
        server.Register<DungeonForce>("dungeon_force");
        server.Register<DungeonStart>("dungeon_start");

        server.Register<RaidStreamer>("raid_streamer");

        server.Register<Craft>("craft");

        server.Register<FerryEnter>("ferry_enter");
        server.Register<FerryLeave>("ferry_leave");
        server.Register<FerryTravel>("ferry_travel");

        server.Register<ItemDropEvent>("item_drop_event");
        server.Register<PlayerCount>("player_count");

        server.Start();
    }
}
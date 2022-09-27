using System;
using UnityEngine;

public class RavenBot : MonoBehaviour
{
    public string RemoteServer;
    public bool UseRemoteBot = true;
    public RavenBotConnection Connection { get; private set; }

    private GameManager gameManager;

    public BotState State { get; set; }

    public void Start()
    {
        if (!this.gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    public void Initialize(GameManager gameManager)
    {
        if (!this.gameManager) this.gameManager = gameManager;
        Connection = new RavenBotConnection(gameManager, this, RemoteServer);
        Connection.Register<KickPlayer>("kick");
        Connection.Register<PlayerLeave>("leave");
        Connection.Register<IslandInfo>("island_info");
        Connection.Register<TrainingInfo>("train_info");
        Connection.Register<AppearanceChange>("change_appearance");
        Connection.Register<PlayerInspect>("inspect");
        Connection.Register<PlayerJoin>("join");
        Connection.Register<PlayerUnstuck>("unstuck");
        Connection.Register<PlayerTask>("task");
        Connection.Register<ObservePlayer>("observe");
        Connection.Register<MonsterPlayer>("monster");
        Connection.Register<ToggleHelmetVisibility>("toggle_helmet");
        Connection.Register<CycleEquippedPet>("toggle_pet");
        Connection.Register<BuyItem>("buy_item");
        Connection.Register<SellItem>("sell_item");
        Connection.Register<VendorItem>("vendor_item");
        Connection.Register<GiftItem>("gift_item");
        Connection.Register<ValueItem>("value_item");
        Connection.Register<CraftRequirement>("req_item");
        Connection.Register<MaxMultiplier>("multiplier");
        Connection.Register<ReloadGame>("reload");
        Connection.Register<RestartGame>("restart");
        Connection.Register<SetPet>("set_pet");
        Connection.Register<GetPet>("get_pet");
        Connection.Register<EquipItem>("equip");
        Connection.Register<UnequipItem>("unequip");
        Connection.Register<EnchantItem>("enchant");
        Connection.Register<PlayerStats>("player_stats");
        Connection.Register<PlayerResources>("player_resources");
        Connection.Register<Highscore>("highscore");
        Connection.Register<HighestSkill>("highest_skill");
        Connection.Register<PlayerScale>("set_player_scale");
        Connection.Register<TwitchSubscriber>("twitch_sub");
        Connection.Register<TwitchCheerer>("twitch_cheer");
        Connection.Register<ArenaJoin>("arena_join");
        Connection.Register<ArenaLeave>("arena_leave");
        Connection.Register<ArenaKick>("arena_kick");
        Connection.Register<ArenaAdd>("arena_add");
        Connection.Register<ArenaBegin>("arena_begin");
        Connection.Register<ArenaEnd>("arena_end");
        Connection.Register<DuelPlayer>("duel");
        Connection.Register<ToggleDiaperMode>("toggle_diaper_mode");
        Connection.Register<ToggleItemRequirements>("toggle_item_requirements");
        Connection.Register<UseExpMultiplierScroll>("use_exp_scroll");
        Connection.Register<SetExpMultiplier>("exp_multiplier");
        Connection.Register<SetExpMultiplierLimit>("exp_multiplier_limit");
        Connection.Register<SetTimeOfDay>("set_time");
        Connection.Register<DuelCancel>("duel_cancel");
        Connection.Register<DuelAccept>("duel_accept");
        Connection.Register<DuelDecline>("duel_decline");
        Connection.Register<RaidJoin>("raid_join");
        Connection.Register<RaidForce>("raid_force");
        Connection.Register<DungeonJoin>("dungeon_join");
        Connection.Register<DungeonForce>("dungeon_force");
        Connection.Register<DungeonStop>("dungeon_stop");
        Connection.Register<DungeonStart>("dungeon_start");
        Connection.Register<RaidStreamer>("raid_streamer");
        Connection.Register<RaidStop>("raid_stop");
        Connection.Register<Craft>("craft");
        Connection.Register<FerryEnter>("ferry_enter");
        Connection.Register<FerryLeave>("ferry_leave");
        Connection.Register<FerryTravel>("ferry_travel");
        Connection.Register<ItemDropEvent>("item_drop_event");
        Connection.Register<PlayerCount>("player_count");
        Connection.Register<RedeemStreamerToken>("redeem_tokens");
        Connection.Register<GetTokenCount>("token_count");
        Connection.Register<GetScrollsCount>("scrolls_count");
        Connection.Register<GetVillageBoost>("get_village_boost");
        Connection.Register<SetVillageHuts>("set_village_huts");
        Connection.Register<TicTacToeActivate>("ttt_activate");
        Connection.Register<TicTacToePlay>("ttt_play");
        Connection.Register<TicTacToeReset>("ttt_reset");
        Connection.Register<PetRacingPlay>("pet_race_play");
        Connection.Register<PetRacingReset>("pet_race_reset");
        Connection.Register<ConnectionPing>("ping");

        Connection.Register<OnsenJoin>("onsen_join");
        Connection.Register<OnsenLeave>("onsen_leave");
        Connection.Register<RestedStatus>("rested_status");
        Connection.Register<ClientVersion>("client_version");


        Connection.LocalConnected -= BotConnected;
        Connection.LocalConnected += BotConnected;
        Connection.LocalDisconnected -= BotDisconnected;
        Connection.LocalDisconnected += BotDisconnected;
        Connection.RemoteConnected -= BotConnected;
        Connection.RemoteConnected += BotConnected;
        Connection.RemoteDisconnected -= BotDisconnected;
        Connection.RemoteDisconnected += BotDisconnected;
        Connection.UseRemoteBot = UseRemoteBot;

        Connection.DataSent += Connection_DataSent;
        Connection.Connect(BotConnectionType.Local);
        Connection.Connect(BotConnectionType.Remote);
    }

    private void Connection_DataSent(object sender, string data)
    {
        if (State == BotState.Disconnected)
        {
            State = BotState.Connected;
        }

        //#if UNITY_EDITOR
        //        UnityEngine.Debug.Log("Sent to Bot: " + data);
        //#endif
        //if (State != BotState.Disconnected)
        //{
        //    State = BotState.Ready;
        //}
    }

    private void BotDisconnected(object sender, GameClient e)
    {
        State = BotState.Disconnected;
        if ((!Connection.IsConnectedToLocal || e.IsLocal) && !Connection.IsConnectedToRemote)
        {
            Connection.Connect(BotConnectionType.Remote);
        }
    }

    private void BotConnected(object sender, GameClient e)
    {
        if (State == BotState.Disconnected)
        {
            State = BotState.Connected;
        }

        if (e.IsLocal && Connection.IsConnectedToRemote)
        {
            Connection.Disconnect(BotConnectionType.Remote);
        }

        UpdateSessionInfo();
    }

    internal void UpdateSessionInfo()
    {
        if (!gameManager)
        {
            return;
        }

        if (this.Connection != null && gameManager.RavenNest != null)
        {
            this.Connection.SendSessionOwner(
                gameManager.RavenNest.TwitchUserId,
                gameManager.RavenNest.TwitchUserName,
                gameManager.RavenNest.SessionId);
        }
    }

    private void OnDestroy()
    {
        this.Connection.Stop(false);
    }
}

public enum BotState
{
    Disconnected,
    Connected,
    Ready
}
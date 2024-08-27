using RavenNest.SDK;

public class RavenBot : System.IDisposable
{
    public bool UseRemoteBot = true;
    public RavenBotConnection Connection { get; private set; }

    private readonly GameManager gameManager;

    private readonly RavenNest.SDK.ILogger logger;

    public BotState State { get; set; }

    public bool HasJoinedChannel { get; set; }

    public RavenBot(RavenNest.SDK.ILogger logger, RavenNestClient ravenNest, GameManager gameManager)
    {
        if (!this.gameManager) this.gameManager = gameManager;
        this.logger = logger;

        Connection = new RavenBotConnection(logger, ravenNest, gameManager, this);
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

        Connection.Register<BuyItemFromMarket>("buy_item");
        Connection.Register<PutItemOnMarket>("sell_item");
        Connection.Register<SellItemToVendorVendor>("vendor_item");

        Connection.Register<UseVendor>("vendor");
        Connection.Register<UseMarketplace>("marketplace");

        Connection.Register<OnChatMessage>("chat_message");

        Connection.Register<GiftItem>("gift_item");
        Connection.Register<SendItem>("send_item");
        Connection.Register<ValueItem>("value_item");
        Connection.Register<CraftRequirement>("req_item");
        Connection.Register<ItemUsage>("item_usage");


        Connection.Register<MaxMultiplier>("multiplier");

        Connection.Register<UpdateGame>("update");
        Connection.Register<ReloadGame>("reload");
        Connection.Register<RestartGame>("restart");

        Connection.Register<SetPet>("set_pet");
        Connection.Register<GetPet>("get_pet");
        Connection.Register<EquipItem>("equip");
        Connection.Register<UnequipItem>("unequip");
        Connection.Register<EnchantItem>("enchant");

        Connection.Register<ClearEnchantmentCooldown>("clear_enchantment_cooldown");
        Connection.Register<EnchantmentCooldown>("enchantment_cooldown");

        Connection.Register<DisenchantItem>("disenchant");
        Connection.Register<PlayerStats>("player_stats");
        Connection.Register<PlayerEq>("player_eq");

        Connection.Register<PlayerResources>("player_resources");
        Connection.Register<TownResources>("town_resources");
        Connection.Register<TownStats>("village_stats");

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

        Connection.Register<RaidAuto>("raid_auto");
        Connection.Register<DungeonAuto>("dungeon_auto");

        Connection.Register<RaidGetCombatStyle>("raid_skill_get");
        Connection.Register<DungeonGetCombatStyle>("dungeon_skill_get");
        Connection.Register<DungeonCombatStyleClear>("dungeon_skill_clear");
        Connection.Register<DungeonCombatStyleSet>("dungeon_skill");
        Connection.Register<RaidCombatStyleClear>("raid_skill_clear");
        Connection.Register<RaidCombatStyleSet>("raid_skill");

        Connection.Register<DungeonJoin>("dungeon_join");
        Connection.Register<DungeonForce>("dungeon_force");

        Connection.Register<DungeonProceed>("dungeon_proceed");
        Connection.Register<DungeonKillBoss>("dungeon_kill_boss");

        Connection.Register<DungeonStop>("dungeon_stop");
        Connection.Register<DungeonStart>("dungeon_start");
        
        Connection.Register<RaidKillBoss>("raid_kill_boss");

        Connection.Register<RaidStreamer>("raid_streamer");
        Connection.Register<RaidStop>("raid_stop");

        Connection.Register<Craft>("craft");
        Connection.Register<Brew>("brew");
        Connection.Register<Cook>("cook");

        Connection.Register<Mine>("mine");
        Connection.Register<Farm>("farm");
        Connection.Register<Fish>("fish");
        Connection.Register<Chop>("chop");
        Connection.Register<Gather>("gather");

        Connection.Register<ClanInfoHandler>("clan_info");
        Connection.Register<ClanStatsHandler>("clan_stats");
        Connection.Register<ClanRank>("clan_rank");
        Connection.Register<ClanJoin>("clan_join");
        Connection.Register<ClanRemove>("clan_remove");
        Connection.Register<ClanLeave>("clan_leave");
        Connection.Register<ClanInvite>("clan_invite");
        Connection.Register<ClanAccept>("clan_accept");
        Connection.Register<ClanDecline>("clan_decline");
        Connection.Register<ClanPromote>("clan_promote");
        Connection.Register<ClanDemote>("clan_demote");

        Connection.Register<ChannelStateChanged>("channel_state");

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

        Connection.Register<ExamineItem>("examine_item");
        Connection.Register<UseItem>("use_item");
        Connection.Register<TeleportToIsland>("teleport_island");

        Connection.Register<GetStatusEffects>("get_status_effects");
        Connection.Register<ItemCount>("get_item_count");
        Connection.Register<OnsenJoin>("onsen_join");
        Connection.Register<OnsenLeave>("onsen_leave");
        Connection.Register<RestedStatus>("rested_status");
        Connection.Register<ClientVersion>("client_version");

        Connection.Register<AutoRest>("auto_rest");
        Connection.Register<AutoRestStop>("auto_rest_stop");
        Connection.Register<AutoRestStatus>("auto_rest_status");
        Connection.Register<AutoUse>("auto_use");
        Connection.Register<AutoUseStop>("auto_use_stop");
        Connection.Register<AutoUseStatus>("auto_use_status");

        Connection.Register<GetLoot>("get_loot");

        Connection.Register<Dps>("dps");

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
        //        Shinobytes.Debug.Log("Sent to Bot: " + data);
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
            this.Connection.SendSessionOwner();
        }
    }

    public void Dispose()
    {
        try
        {
            if (this.Connection != null)
                this.Connection.Stop(false);
        }
        catch
        {
            // ignored
        }
    }
}

public enum BotState
{
    NotSet,
    Disconnected,
    Connected,
    Ready
}

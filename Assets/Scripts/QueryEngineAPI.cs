using Assets.Scripts;
using RavenfallDataPipe;
using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class QueryEngineAPI
{
    private static QueryEngineContext Context;
    private static QueryEngineWebAPIServer Server;


    public static void OnGameManagerAwake(GameManager gm)
    {
        if (!Overlay.IsGame)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

        // Rebuild Context
        Context = RebuildDataContext(gm);

        // Dispose existing pipe server
        if (Server != null)
        {
            OnExit();
        }

        if (PlayerSettings.Instance.QueryEngineEnabled.GetValueOrDefault())
        {
            Server = new QueryEngineWebAPIServer(Context);
            Server.Start(PlayerSettings.Instance.QueryEngineApiPrefix);
        }
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
            change == UnityEditor.PlayModeStateChange.EnteredEditMode)
        {
            OnExit();
        }
    }
#endif

    private static QueryEngineContext RebuildDataContext(GameManager gm)
    {
        var context = new QueryEngineContext();
        var gameVersion = GameVersion.GetApplicationVersion().ToString();
        var started = DateTime.Now;


        context.Register("items", gm.Items.GetItems,
            QueryEngineContext.Column<RavenNest.Models.Item, Guid>("Id", x => x.Id).MakePrimaryKey(),
            QueryEngineContext.Column<RavenNest.Models.Item, string>("Name", x => x.Name),
            QueryEngineContext.Column<RavenNest.Models.Item, string>("Description", x => x.Description),
            QueryEngineContext.Column<RavenNest.Models.Item, string>("Category", x => x.Category.ToString()),
            QueryEngineContext.Column<RavenNest.Models.Item, string>("Type", x => x.Type.ToString()),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("WeaponAim", x => x.WeaponAim),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("WeaponPower", x => x.WeaponPower),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RangedAim", x => x.RangedAim),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RangedPower", x => x.RangedPower),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("MagicAim", x => x.MagicAim),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("MagicPower", x => x.MagicPower),

            QueryEngineContext.Column<RavenNest.Models.Item, int>("RequiredAttackLevel", x => x.RequiredAttackLevel),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RequiredDefenseLevel", x => x.RequiredDefenseLevel),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RequiredMagicLevel", x => x.RequiredMagicLevel),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RequiredRangedLevel", x => x.RequiredRangedLevel),
            QueryEngineContext.Column<RavenNest.Models.Item, int>("RequiredSlayerLevel", x => x.RequiredSlayerLevel),
            QueryEngineContext.Column<RavenNest.Models.Item, bool>("Soulbound", x => x.Soulbound)
        );

        context.Register("redeemables", gm.Items.GetRedeemables,
            QueryEngineContext.Column<RedeemableItem, Guid>("ItemId", x => x.ItemId).MakePrimaryKey(),
            QueryEngineContext.Column<RedeemableItem, string>("Name", x => x.Name),
            QueryEngineContext.Column<RedeemableItem, string>("Description", x => gm.Items.Get(x.ItemId).Description),
            QueryEngineContext.Column<RedeemableItem, string>("Currency", x => x.Currency),
            QueryEngineContext.Column<RedeemableItem, int>("Cost", x => x.Cost)
        );

        var playerColumns = new List<SqlPipeDataContextRowColumn<PlayerController>>
        {
            QueryEngineContext.Column<PlayerController, Guid>("Id", x => x.Id).MakePrimaryKey(),
            QueryEngineContext.Column<PlayerController, string>("Name", x => x.Name),
            QueryEngineContext.Column<PlayerController, string>("Training", x => x.ActiveSkill.ToString()),
            QueryEngineContext.Column<PlayerController, string>("TaskArgument", x => x.taskArgument),
            QueryEngineContext.Column<PlayerController, string>("Island", x => x.Island?.Identifier ?? ""),
            QueryEngineContext.Column<PlayerController, bool>("Sailing", x => x.ferryHandler.OnFerry),
            QueryEngineContext.Column<PlayerController, bool>("Resting", x => x.onsenHandler.InOnsen),
            QueryEngineContext.Column<PlayerController, double>("RestedTime", x => x.Rested.RestedTime),
            QueryEngineContext.Column<PlayerController, bool>("InArena", x => x.arenaHandler.InArena),
            QueryEngineContext.Column<PlayerController, bool>("InDuel", x => x.duelHandler.InDuel),
            QueryEngineContext.Column<PlayerController, bool>("InDungeon", x => x.dungeonHandler.InDungeon),
            QueryEngineContext.Column<PlayerController, bool>("InRaid", x => x.raidHandler.InRaid),
            QueryEngineContext.Column<PlayerController, long>("Coins", x => (long)x.Resources.Coins),
            QueryEngineContext.Column<PlayerController, double>("CommandIdleTime", x => x.TimeSinceLastChatCommandUtc.TotalSeconds),
            QueryEngineContext.Column<PlayerController, int>("Stats.CombatLevel", x => x.Stats.CombatLevel),
        };

        foreach (var skill in SkillUtilities.Skills)
        {
            if (skill == Skill.Melee || skill == Skill.None) continue;
            playerColumns.Add(QueryEngineContext.Column<PlayerController, int>("Stats." + skill + ".Level", x => x.Stats.GetSkill(skill).Level));
            playerColumns.Add(QueryEngineContext.Column<PlayerController, int>("Stats." + skill + ".CurrentValue", x => x.Stats.GetSkill(skill).CurrentValue));
            playerColumns.Add(QueryEngineContext.Column<PlayerController, int>("Stats." + skill + ".MaxLevel", x => x.Stats.GetSkill(skill).MaxLevel));
            playerColumns.Add(QueryEngineContext.Column<PlayerController, double>("Stats." + skill + ".Experience", x => x.Stats.GetSkill(skill).Experience));
        }

        context.Register("players", gm.Players.GetAllPlayers, playerColumns.ToArray());

        context.Register("observed", () => new[] { gm.ObservedPlayerDetails.ObservedPlayer }, playerColumns.ToArray()).HasOnlyOneRow();

        context.Register("session", () => new[] { gm },
            QueryEngineContext.Column<GameManager, bool>("Authenticated", x => x.RavenNest.Authenticated),
            QueryEngineContext.Column<GameManager, bool>("SessionStarted", x => x.RavenNest.SessionStarted),
            QueryEngineContext.Column<GameManager, string>("TwitchUserName", x => x.RavenNest.TwitchUserName),
            QueryEngineContext.Column<GameManager, int>("Players", x => x.Players.GetPlayerCount()),
            QueryEngineContext.Column<GameManager, string>("GameVersion", x => gameVersion),
            QueryEngineContext.Column<GameManager, double>("SecondsSinceStart", x => (DateTime.Now - started).TotalSeconds)
        ).HasOnlyOneRow();

        context.Register("ferry", () => new[] { gm.Ferry },
            QueryEngineContext.Column<FerryController, string>("Destination", x => x.GetDestination()),
            QueryEngineContext.Column<FerryController, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<FerryController, string>("Captain.Name", x => x.Captain?.Name ?? ""),
            QueryEngineContext.Column<FerryController, int>("Captain.SailingLevel", x => x.Captain?.Stats.Sailing.MaxLevel ?? 0)
        ).HasOnlyOneRow();

        context.Register("village", () => new[] { gm.Village },
            QueryEngineContext.Column<VillageManager, string>("Name", x => "My Village"),
            QueryEngineContext.Column<VillageManager, int>("Level", x => x.TownHall.Level),
            QueryEngineContext.Column<VillageManager, int>("Tier", x => x.TownHall.Tier),
            QueryEngineContext.Column<VillageManager, string>("Boost", x => gm.GetVillageBoostString())
        ).HasOnlyOneRow();

        context.Register("multiplier", () => new[] { gm.GetExpMultiplierStats() },
            QueryEngineContext.Column<ExpBoostEvent, string>("EventName", x => x.EventName),
            QueryEngineContext.Column<ExpBoostEvent, bool>("Active", x => x.Active),
            QueryEngineContext.Column<ExpBoostEvent, float>("Multiplier", x => x.Multiplier),
            QueryEngineContext.Column<ExpBoostEvent, double>("Elapsed", x => x.Elapsed.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, double>("Duration", x => x.Duration.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, double>("TimeLeft", x => x.TimeLeft.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, string>("StartTime", x => x.StartTime.ToString()),
            QueryEngineContext.Column<ExpBoostEvent, string>("EndTime", x => x.EndTime.ToString())
        ).HasOnlyOneRow();

        context.Register("dungeon", () => new[] { gm.Dungeons },
            QueryEngineContext.Column<DungeonManager, bool>("Started", x => x.Started),
            QueryEngineContext.Column<DungeonManager, float>("SecondsUntilStart", x => x.SecondsUntilStart),
            QueryEngineContext.Column<DungeonManager, string>("Name", x => x.Dungeon?.Name ?? ""),
            QueryEngineContext.Column<DungeonManager, int>("Room", x => x.Dungeon?.CurrentRoomIndex ?? 0),
            QueryEngineContext.Column<DungeonManager, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<DungeonManager, int>("PlayersAlive", x => x.GetAlivePlayerCount()),
            QueryEngineContext.Column<DungeonManager, int>("Enemies", x => x.GetEnemyCount()),
            QueryEngineContext.Column<DungeonManager, int>("EnemiesAlive", x => x.GetAliveEnemyCount()),
            QueryEngineContext.Column<DungeonManager, double>("Elapsed", x => x.Elapsed.TotalSeconds),
            QueryEngineContext.Column<DungeonManager, int>("Count", x => x.Counter),
            QueryEngineContext.Column<DungeonManager, int>("Boss.Health", x => x.Boss?.Enemy.Stats.Health.CurrentValue ?? 0),
            QueryEngineContext.Column<DungeonManager, int>("Boss.MaxHealth", x => x.Boss?.Enemy.Stats.Health.MaxLevel ?? 0),
            QueryEngineContext.Column<DungeonManager, float>("Boss.HealthPercent", x => x.Boss?.Enemy.Stats.HealthPercent ?? 0f),
            QueryEngineContext.Column<DungeonManager, int>("Boss.CombatLevel", x => x.Boss?.Enemy.Stats.CombatLevel ?? 0)
        ).HasOnlyOneRow();

        context.Register("raid", () => new[] { gm.Raid },
            QueryEngineContext.Column<RaidManager, bool>("Started", x => x.Started),
            QueryEngineContext.Column<RaidManager, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<RaidManager, float>("TimeLeft", x => x.SecondsLeft),
            QueryEngineContext.Column<RaidManager, int>("Count", x => x.Counter),
            QueryEngineContext.Column<RaidManager, int>("Boss.Health", x => x.Boss?.Enemy.Stats.Health.CurrentValue ?? 0),
            QueryEngineContext.Column<RaidManager, int>("Boss.MaxHealth", x => x.Boss?.Enemy.Stats.Health.MaxLevel ?? 0),
            QueryEngineContext.Column<RaidManager, float>("Boss.HealthPercent", x => x.Boss?.Enemy.Stats.HealthPercent ?? 0f),
            QueryEngineContext.Column<RaidManager, int>("Boss.CombatLevel", x => x.Boss?.Enemy.Stats.CombatLevel ?? 0)
        ).HasOnlyOneRow();

        context.Register("settings", () => new[] { PlayerSettings.Instance },
            QueryEngineContext.Column<PlayerSettings, int>("Game.PlayerCacheExpiryTimeIndex", x => x.PlayerCacheExpiryTime.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, float>("Game.CameraRotationSpeed", x => x.CameraRotationSpeed.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, float>("Game.DayNightTime", x => x.DayNightTime.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Game.DayNightCycleEnabled", x => x.DayNightCycleEnabled.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Game.RealTimeDayNightCycle", x => x.RealTimeDayNightCycle.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Game.AutoKickAfkPlayers", x => x.AutoKickAfkPlayers.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Game.LocalBotServerDisabled", x => x.LocalBotServerDisabled.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Game.AlertExpiredStateCacheInChat", x => x.AlertExpiredStateCacheInChat.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, int>("Game.PlayerBoostRequirement", x => x.PlayerBoostRequirement.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, int>("Game.ItemDropMessageType", x => x.ItemDropMessageType.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, int>("Game.PathfindingQualitySettings", x => x.PathfindingQualitySettings.GetValueOrDefault()),

            QueryEngineContext.Column<PlayerSettings, float>("Sound.MusicVolume", x => x.MusicVolume.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, float>("Sound.RaidHornVolume", x => x.RaidHornVolume.GetValueOrDefault()),

            QueryEngineContext.Column<PlayerSettings, bool>("UI.PlayerNamesVisible", x => x.PlayerNamesVisible.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, float>("UI.PlayerListSize", x => x.PlayerListSize.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, float>("UI.PlayerListScale", x => x.PlayerListScale.GetValueOrDefault()),

            QueryEngineContext.Column<PlayerSettings, float>("Graphics.DPIScale", x => x.DPIScale.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Graphics.PotatoMode", x => x.PotatoMode.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Graphics.AutoPotatoMode", x => x.AutoPotatoMode.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("Graphics.PostProcessing", x => x.PostProcessing.GetValueOrDefault()),

            QueryEngineContext.Column<PlayerSettings, bool>("QueryEngine.Enabled", x => x.QueryEngineEnabled.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, bool>("QueryEngine.AlwaysReturnArray", x => x.QueryEngineAlwaysReturnArray.GetValueOrDefault()),
            QueryEngineContext.Column<PlayerSettings, string>("QueryEngine.ApiPrefix", x => x.QueryEngineApiPrefix),

            QueryEngineContext.Column<PlayerSettings, bool>("StreamLabels.Enabled", x => x.StreamLabels.Enabled),
            QueryEngineContext.Column<PlayerSettings, bool>("StreamLabels.SaveTextFiles", x => x.StreamLabels.SaveTextFiles),
            QueryEngineContext.Column<PlayerSettings, bool>("StreamLabels.SaveJsonFiles", x => x.StreamLabels.SaveJsonFiles),

            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.Default", x => x.PlayerObserveSeconds.Default),
            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.Subscriber", x => x.PlayerObserveSeconds.Subscriber),
            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.Moderator", x => x.PlayerObserveSeconds.Moderator),
            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.Vip", x => x.PlayerObserveSeconds.Vip),

            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.Broadcaster", x => x.PlayerObserveSeconds.Broadcaster),
            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.OnSubcription", x => x.PlayerObserveSeconds.OnSubcription),
            QueryEngineContext.Column<PlayerSettings, float>("PlayerObserveSeconds.OnCheeredBits", x => x.PlayerObserveSeconds.OnCheeredBits)
        ).HasOnlyOneRow();


        gm.Islands.EnsureIslands();

        var islandColumns = new List<SqlPipeDataContextRowColumn<IslandController>>
        {
            QueryEngineContext.Column<IslandController, string>("Name", x => x.Identifier).MakePrimaryKey(),
            QueryEngineContext.Column<IslandController, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<IslandController, int>("Level.Skill", x => {
                if (x.Island == Island.Home) return 1;
                var chunks = gm.Chunks.GetChunks(x);
                if (chunks.Count == 0) return -1;
                return chunks.Max(x => x.RequiredSkilllevel);
            }),
            QueryEngineContext.Column<IslandController, int>("Level.Combat", x => {
                var chunks = gm.Chunks.GetChunks(x);
                if (x.Island == Island.Home) return 1;
                if (chunks.Count == 0) return -1;
                return chunks.Max(x => x.RequiredCombatLevel);
            }),
            // SqlPipeDataContext.Column<IslandController, int>("Level.Skill", x => x.),
        };

        //var requirements = gm.GetLevelRequirements();
        //foreach (var req in requirements)
        //{
        //    req.Skills
        //}

        context.Register("islands", () => gm.Islands.All, islandColumns.ToArray());
        context.Register("tables", context.GetTables, context.GetColumns());

        return context;
    }

    public static void OnExit()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        if (Server != null)
        {
            Server.Dispose();
            Shinobytes.Debug.Log("PipeSQL Stopped");
        }
    }
}

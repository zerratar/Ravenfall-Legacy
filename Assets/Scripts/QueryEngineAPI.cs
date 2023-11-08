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
            QueryEngineContext.Column<RedeemableItem, int>("Cost", x => x.Cost)
        );

        var playerColumns = new List<SqlPipeDataContextRowColumn<PlayerController>>
        {
            QueryEngineContext.Column<PlayerController, Guid>("Id", x => x.Id).MakePrimaryKey(),
            QueryEngineContext.Column<PlayerController, string>("Name", x => x.Name),
            QueryEngineContext.Column<PlayerController, string>("Training", x => x.ActiveSkill.ToString()),
            QueryEngineContext.Column<PlayerController, string>("TaskArgument", x => x.taskArgument),
            QueryEngineContext.Column<PlayerController, string>("Island", x => x.Island?.Identifier ?? ""),
            QueryEngineContext.Column<PlayerController, bool>("Sailing", x => x.Ferry.OnFerry),
            QueryEngineContext.Column<PlayerController, bool>("Resting", x => x.Onsen.InOnsen),
            QueryEngineContext.Column<PlayerController, double>("RestedTime", x => x.Rested.RestedTime),
            QueryEngineContext.Column<PlayerController, bool>("InArena", x => x.Arena.InArena),
            QueryEngineContext.Column<PlayerController, bool>("InDuel", x => x.Duel.InDuel),
            QueryEngineContext.Column<PlayerController, bool>("InDungeon", x => x.Dungeon.InDungeon),
            QueryEngineContext.Column<PlayerController, bool>("InRaid", x => x.Raid.InRaid),
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

        context.Register("session", () => new[] { gm },
            QueryEngineContext.Column<GameManager, bool>("Authenticated", x => x.RavenNest.Authenticated),
            QueryEngineContext.Column<GameManager, bool>("SessionStarted", x => x.RavenNest.SessionStarted),
            QueryEngineContext.Column<GameManager, string>("TwitchUserName", x => x.RavenNest.TwitchUserName),
            QueryEngineContext.Column<GameManager, int>("Players", x => x.Players.GetPlayerCount()),
            QueryEngineContext.Column<GameManager, string>("GameVersion", x => gameVersion),
            QueryEngineContext.Column<GameManager, double>("SecondsSinceStart", x => (DateTime.Now - started).TotalSeconds)
        );

        context.Register("ferry", () => new[] { gm.Ferry },
            QueryEngineContext.Column<FerryController, string>("Destination", x => x.GetDestination()),
            QueryEngineContext.Column<FerryController, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<FerryController, string>("Captain.Name", x => x.Captain?.Name ?? ""),
            QueryEngineContext.Column<FerryController, int>("Captain.SailingLevel", x => x.Captain?.Stats.Sailing.MaxLevel ?? 0)
        );

        context.Register("village", () => new[] { gm.Village },
            QueryEngineContext.Column<VillageManager, string>("Name", x => "My Village"),
            QueryEngineContext.Column<VillageManager, int>("Level", x => x.TownHall.Level),
            QueryEngineContext.Column<VillageManager, int>("Tier", x => x.TownHall.Tier),
            QueryEngineContext.Column<VillageManager, string>("Boost", x => gm.GetVillageBoostString())
        );

        context.Register("multiplier", () => new[] { gm.GetExpMultiplierStats() },
            QueryEngineContext.Column<ExpBoostEvent, string>("EventName", x => x.EventName),
            QueryEngineContext.Column<ExpBoostEvent, bool>("Active", x => x.Active),
            QueryEngineContext.Column<ExpBoostEvent, float>("Multiplier", x => x.Multiplier),
            QueryEngineContext.Column<ExpBoostEvent, double>("Elapsed", x => x.Elapsed.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, double>("Duration", x => x.Duration.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, double>("TimeLeft", x => x.TimeLeft.TotalSeconds),
            QueryEngineContext.Column<ExpBoostEvent, string>("StartTime", x => x.StartTime.ToString()),
            QueryEngineContext.Column<ExpBoostEvent, string>("EndTime", x => x.EndTime.ToString())
        );

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
        );

        context.Register("raid", () => new[] { gm.Raid },
            QueryEngineContext.Column<RaidManager, bool>("Started", x => x.Started),
            QueryEngineContext.Column<RaidManager, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<RaidManager, float>("TimeLeft", x => x.SecondsLeft),
            QueryEngineContext.Column<RaidManager, int>("Count", x => x.Counter),
            QueryEngineContext.Column<RaidManager, int>("Boss.Health", x => x.Boss?.Enemy.Stats.Health.CurrentValue ?? 0),
            QueryEngineContext.Column<RaidManager, int>("Boss.MaxHealth", x => x.Boss?.Enemy.Stats.Health.MaxLevel ?? 0),
            QueryEngineContext.Column<RaidManager, float>("Boss.HealthPercent", x => x.Boss?.Enemy.Stats.HealthPercent ?? 0f),
            QueryEngineContext.Column<RaidManager, int>("Boss.CombatLevel", x => x.Boss?.Enemy.Stats.CombatLevel ?? 0)
        );

        gm.Islands.EnsureIslands();

        var islandColumns = new List<SqlPipeDataContextRowColumn<IslandController>>
        {
            QueryEngineContext.Column<IslandController, string>("Name", x => x.Identifier).MakePrimaryKey(),
            QueryEngineContext.Column<IslandController, int>("Players", x => x.GetPlayerCount()),
            QueryEngineContext.Column<IslandController, int>("Level.Skill", x => {
                var chunks = gm.Chunks.GetChunks(x);
                if (chunks.Count == 0) return -1;
                return chunks.Min(x => x.RequiredSkilllevel);
            }),
            QueryEngineContext.Column<IslandController, int>("Level.Combat", x => {
                                var chunks = gm.Chunks.GetChunks(x);
                if (chunks.Count == 0) return -1;
                return chunks.Min(x => x.RequiredCombatLevel);
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

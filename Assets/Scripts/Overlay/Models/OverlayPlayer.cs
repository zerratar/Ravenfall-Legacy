using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class OverlayPlayer
{
    public OverlayPlayer() { }
    public OverlayPlayer(PlayerController source)
    {
        this.Character = RebuildDefinition(source);
        this.Twitch = BuildTwitchUser(source);
    }

    public Player Character { get; set; }
    public User Twitch { get; set; }

    private static User BuildTwitchUser(PlayerController source)
    {
        if (source.User == null)
        {
            return new User
            {
                Color = source.PlayerNameHexColor,
                DisplayName = source.PlayerName,
                Identifier = source.CharacterIndex.ToString(),
                IsBroadcaster = source.IsBroadcaster,
                IsModerator = source.IsModerator,
                IsSubscriber = source.IsSubscriber,
                IsGameAdministrator = source.IsGameAdmin,
                IsGameModerator = source.IsGameModerator,
                IsVip = source.IsVip,
                Id = source.UserId,
                Platform = source.Platform,
                PlatformId = source.PlatformId,
                Username = source.Name
            };
        }

        return source.User; // we will assume its always up to date.
    }

    private static Player RebuildDefinition(PlayerController source)
    {
        var player = new Player();
        player.Id = source.Id;
        player.Identifier = source.Definition.Identifier;
        player.OriginUserId = source.Definition.OriginUserId;
        player.PatreonTier = source.PatreonTier;

        player.UserId = source.UserId;
        player.UserName = source.Definition.UserName;
        player.Name = source.Name;
        player.Clan = source.clanHandler.ClanInfo;
        player.ClanRole = source.clanHandler.Role;
        player.IsAdmin = source.IsGameAdmin;
        player.IsModerator = source.IsGameModerator;
        player.Connections = source.Definition.Connections ?? new List<AuthServiceConnection>();
        player.Appearance = GetAppearance(source.Appearance);
        player.State = GetState(source);
        player.Skills = GetSkills(source);
        player.InventoryItems = GetInventoryItems(source);
        player.Resources = GetResources(source);

        return player;
    }

    private static Resources GetResources(PlayerController source)
    {
        return source.Resources;
    }

    private static IReadOnlyList<InventoryItem> GetInventoryItems(PlayerController source)
    {
        return source.Inventory.GetInventoryItems();
    }

    private static RavenNest.Models.Skills GetSkills(PlayerController source)
    {
        return source.Stats.ToServerModel();
    }

    private static CharacterState GetState(PlayerController source)
    {
        return new CharacterState
        {
            RestedTime = source.Rested.RestedTime,
            Health = source.Stats.Health.CurrentValue,
            InArena = source.arenaHandler.InArena,
            InDungeon = source.dungeonHandler.InDungeon,
            InOnsen = source.onsenHandler.InOnsen,
            InRaid = source.raidHandler.InRaid,
            Island = source.Island?.name,
            Task = source.GetTask().ToString(), //source.Chunk?.ChunkType.ToString(),
            TaskArgument = source.GetTaskArgument(),
            X = source.Position.x,
            Y = source.Position.y,
            Z = source.Position.z
        };
    }

    private static SyntyAppearance GetAppearance(SyntyPlayerAppearance appearance)
    {
        if (appearance != null)
        {
            return appearance.ToSyntyAppearanceData();
        }

        throw new NotImplementedException();
    }
}
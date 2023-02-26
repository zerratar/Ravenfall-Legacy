using RavenNest.Models;
using Shinobytes.Linq;
using System;

public class DuelCancel : ChatBotCommandHandler<TwitchPlayerInfo>
{
    public DuelCancel(
        GameManager game,
        RavenBotConnection server,
        PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(TwitchPlayerInfo data, GameClient client)
    {
    }
}

public class ClanInfoHandler : ChatBotCommandHandler<PlayerAndString>
{
    public ClanInfoHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clanInfo = await Game.RavenNest.Clan.GetClanInfoAsync(plr.Id);
        if (clanInfo == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_CLAN_INFO_UNKNOWN_ERROR);
            return;
        }

        client.SendMessage(data.Player.Username, GenerateStringPresentation(clanInfo));
    }

    private string GenerateStringPresentation(ClanInfo data)
    {
        var msg = $"Your clan is {data.Name} led by {data.OwnerName}. There are currently ";
        var totalMemberCount = data.Roles.Sum(x => x.MemberCount);
        msg += string.Join(", ", data.Roles.OrderByDescending(x => x.Level).Select(x =>
        {
            var str = x.MemberCount + " " + x.Name;
            if (x.MemberCount > 1) str += "s";
            return str;
        }));

        msg += ". A total of " + totalMemberCount + " members.";
        return msg;
    }
}


public class ClanStatsHandler : ChatBotCommandHandler<PlayerAndString>
{
    public ClanStatsHandler(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }
    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clanData = await Game.RavenNest.Clan.GetClanStatsAsync(plr.Id);
        if (clanData == null)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_CLAN_STATS_UNKNOWN_ERROR);
            return;
        }

        client.SendMessage(data.Player.Username, GenerateStringPresentation(clanData));
    }

    private string GenerateStringPresentation(ClanStats data)
    {
        var msg = $"Your clan is {data.Name} led by {data.OwnerName} and is currently level {data.Level}.";
        if (data.ClanSkills != null)
        {
            if (data.ClanSkills.Count == 1)
            {
                var ench = data.ClanSkills[0];
                return msg + $" Your clan has an enchanting level of {ench.Level}.";
            }

            var skills = string.Join(", ", data.ClanSkills.Select(x => x.Name + " level " + x.Level));

            return msg + $" Your clan has the following skills {skills}.";
        }
        return msg;
    }
}


public class ClanLeave : ChatBotCommandHandler<PlayerAndString>
{
    public ClanLeave(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var clan = plr.Clan;
        var result = await Game.RavenNest.Clan.LeaveAsync(plr.Id);
        if (result.Success)
        {
            client.SendMessage(data.Player.Username, $"You have departed your clan and are no longer a member of {clan.ClanInfo.Name}.");
            clan.Leave();
            return;
        }

        client.SendMessage(data.Player.Username,
            result.ErrorMessage ??
            "Unable to leave your clan right now, unknown error. Please try again later.");
    }
}


public class ClanJoin : ChatBotCommandHandler<PlayerAndString>
{
    public ClanJoin(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan != null && plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ALREADY_IN_CLAN);
            return;
        }

        var owner = Game.RavenNest.TwitchUserId;
        if (!string.IsNullOrEmpty(data.Value))
        {
            var otherPlayer = PlayerManager.GetPlayerByName(data.Value);
            if (otherPlayer != null && otherPlayer.Clan.InClan && otherPlayer.Clan.ClanInfo != null)
            {
                owner = otherPlayer.Clan.ClanInfo.Owner;
            }
            else
            {
                var clan = Game.Clans.GetByName(data.Value);
                if (clan != null)
                {
                    owner = clan.Owner;
                }
            }
        }

        var result = await Game.RavenNest.Clan.JoinAsync(owner, plr.Id);
        if (result.Success)
        {
            plr.Clan.Join(result.Clan, result.Role);

            if (!string.IsNullOrEmpty(result.WelcomeMessage))
            {
                client.SendMessage(data.Player.Username,
                    result.WelcomeMessage
                    .Replace("{ClanName}", result.Clan.Name)
                    .Replace("{PlayerName}", plr.Name)
                    .Replace("{RoleName}", result.Role.Name)
                );
                return;
            }

            client.SendMessage(data.Player.Username, $"You have joined {result.Clan.Name} as a {result.Role.Name}!");
            return;
        }

        client.SendMessage(data.Player.Username, "You are not able to join that clan at this moment.");
    }
}

public class ClanInvite : ChatBotCommandHandler<PlayerAndPlayer>
{
    public ClanInvite(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndPlayer data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var otherPlayer = PlayerManager.GetPlayer(data.TargetPlayer);
        if (!otherPlayer)
        {
            client.SendMessage(data.Player.Username, "No player with name " + data.TargetPlayer.Username + " is currently playing.");
            return;
        }

        if (await Game.RavenNest.Clan.InvitePlayerAsync(plr.Id, otherPlayer.Id))
        {
            client.SendMessage(data.Player.Username, $"Invite was successfully sent. {otherPlayer.Name}, you may use the command '!clan accept' if you wish to join the clan {plr.Clan.ClanInfo.Name}. Or '!clan decline' to decline.");
            return;
        }

        client.SendMessage(data.Player.Username, otherPlayer.Name + " could not be invited right now.");
    }
}
public class ClanRemove : ChatBotCommandHandler<PlayerAndPlayer>
{
    public ClanRemove(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndPlayer data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var otherPlayer = PlayerManager.GetPlayer(data.TargetPlayer);
        if (!otherPlayer)
        {
            client.SendMessage(data.Player.Username, "No player with name " + data.TargetPlayer.Username + " is currently playing.");
            return;
        }

        if (await Game.RavenNest.Clan.RemoveMemberAsync(plr.Id, otherPlayer.Id))
        {
            otherPlayer.Clan.Leave();
            client.SendMessage(data.Player.Username, data.TargetPlayer.Username + " have been removed from the clan.");
            return;
        }

        client.SendMessage(plr.Name, $"You don't have permission to remove {otherPlayer.Name} or the player is not a member of your clan.");
    }
}

public class ClanAccept : ChatBotCommandHandler<PlayerAndString>
{
    public ClanAccept(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan != null && plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_ALREADY_IN_CLAN);
            return;
        }

        var result = await Game.RavenNest.Clan.AcceptInviteAsync(plr.Id, data.Value);
        if (result.Success)
        {
            plr.Clan.Join(result.Clan, result.Role);

            if (!string.IsNullOrEmpty(result.WelcomeMessage))
            {
                client.SendMessage(data.Player.Username,
                    result.WelcomeMessage
                    .Replace("{ClanName}", result.Clan.Name)
                    .Replace("{PlayerName}", plr.Name)
                    .Replace("{RoleName}", result.Role.Name)
                );
                return;
            }

            client.SendMessage(data.Player.Username, $"You have joined {result.Clan.Name} as a {result.Role.Name}!");
            return;
        }

        client.SendMessage(data.Player.Username, "Unable to accept clan invite. Either you don't have any invites or server is bonkers.");
    }
}

public class ClanDecline : ChatBotCommandHandler<PlayerAndString>
{
    public ClanDecline(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        var result = await Game.RavenNest.Clan.DeclineInviteAsync(plr.Id, data.Value);
        if (result.Success)
        {
            client.SendMessage(data.Player.Username, "You have declined the most recent clan invite.");
            return;
        }

        client.SendMessage(data.Player.Username,
            result.ErrorMessage ??
            "Unable to decline clan invite. Either you don't have any invites or server is bonkers.");
        return;
    }
}

public class ClanPromote : ChatBotCommandHandler<PlayerPlayerAndString>
{
    public ClanPromote(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerPlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var otherPlayer = PlayerManager.GetPlayer(data.Target);
        if (!otherPlayer)
        {
            client.SendMessage(data.Player.Username, "No player with name " + data.Target.Username + " is currently playing.");
            return;
        }

        var result = await Game.RavenNest.Clan.PromoteMemberAsync(plr.Id, otherPlayer.Id, data.Value);
        if (result.Success)
        {
            otherPlayer.Clan.SetRole(result.NewRole);
            client.SendMessage(data.Player.Username, $"Congratulations {otherPlayer.Name}! You have been promoted to {result.NewRole.Name}!");
            return;
        }

        client.SendMessage(plr.Name, $"You don't have permission to promote {otherPlayer.Name} or the player is not a member of your clan.");

    }
}

public class ClanDemote : ChatBotCommandHandler<PlayerPlayerAndString>
{
    public ClanDemote(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(PlayerPlayerAndString data, GameClient client)
    {
        var plr = PlayerManager.GetPlayer(data.Player);
        if (!plr)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_PLAYING);
            return;
        }

        if (plr.Clan == null || !plr.Clan.InClan)
        {
            client.SendMessage(data.Player.Username, Localization.MSG_NOT_IN_CLAN);
            return;
        }

        var otherPlayer = PlayerManager.GetPlayer(data.Target);
        if (!otherPlayer)
        {
            client.SendMessage(data.Player.Username, "No player with name " + data.Target.Username + " is currently playing.");
            return;
        }

        var result = await Game.RavenNest.Clan.DemoteMemberAsync(plr.Id, otherPlayer.Id, data.Value);
        if (result.Success)
        {
            otherPlayer.Clan.SetRole(result.NewRole);
            client.SendMessage(data.Player.Username, $"{otherPlayer.Name} has been demoted to {result.NewRole.Name}");
            return;
        }

        client.SendMessage(plr.Name, $"You don't have permission to demote {otherPlayer.Name} or the player is not a member of your clan.");
    }
}

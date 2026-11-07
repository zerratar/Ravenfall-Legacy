using System;
using System.Globalization;
using RavenNest.Combat;

/// <summary>
/// The tower from chat (docs/tower-design.md). The bot sends "tower_command" with the action first: !tower (enter the
/// stream's floor, or where it stands when already in), !tower leave, !tower boss (the streamer or a moderator starts
/// a boss attempt; anyone in the tower votes for one) and !tower info.
/// </summary>
public class TowerCommand : ChatBotCommandHandler<string>
{
    public TowerCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var tower = TowerManager.Instance;
        if (!tower)
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }

        var text = (data ?? "").Trim().ToLowerInvariant();
        var space = text.IndexOf(' ');
        var action = space < 0 ? text : text.Substring(0, space);
        var user = gm.Sender;
        var player = PlayerManager.GetPlayer(user);

        try
        {
            switch (action)
            {
                case "leave":
                case "exit":
                    Leave(tower, player, gm, client);
                    return;
                case "boss":
                case "challenge":
                    Boss(tower, player, user.IsBroadcaster || user.IsModerator || (player && (player.IsGameAdmin || player.IsGameModerator)), gm, client);
                    return;
                case "info":
                case "status":
                case "floor":
                    Info(tower, gm, client);
                    return;
                default:
                    Join(tower, player, gm, client);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TowerCommand: " + exc);
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
        }
    }

    private static void Join(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var floor = Tower.Get(tower.StreamFloor);
        switch (tower.Join(player))
        {
            case TowerManager.JoinResult.Joined:
                var current = tower.Definition;
                if (tower.Phase == TowerManager.TowerPhase.Countdown)
                    client.SendReply(gm, Localization.MSG_TOWER_JOINED_ATTEMPT, tower.CurrentFloor, ((int)tower.PhaseSecondsLeft).ToString(CultureInfo.InvariantCulture));
                else
                    client.SendReply(gm, Localization.MSG_TOWER_JOINED, tower.CurrentFloor, current != null ? current.Name : "", tower.QuestText());
                return;
            case TowerManager.JoinResult.Loading:
                client.SendReply(gm, Localization.MSG_TOWER_LOADING, tower.StreamFloor, floor != null ? floor.Name : "");
                return;
            case TowerManager.JoinResult.AlreadyIn:
                Info(tower, gm, client);
                return;
            case TowerManager.JoinResult.FightUnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_FIGHT_UNDER_WAY, ((int)tower.PhaseSecondsLeft).ToString(CultureInfo.InvariantCulture));
                return;
            case TowerManager.JoinResult.Busy:
                client.SendReply(gm, Localization.MSG_TOWER_CANNOT_JOIN);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
                return;
        }
    }

    private static void Leave(TowerManager tower, PlayerController player, GameMessage gm, GameClient client)
    {
        if (!player || player.towerHandler == null || !player.towerHandler.InTower)
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_IN);
            return;
        }
        tower.Leave(player);
        client.SendReply(gm, Localization.MSG_TOWER_LEFT);
    }

    private static void Boss(TowerManager tower, PlayerController player, bool canForce, GameMessage gm, GameClient client)
    {
        var floor = Tower.Get(tower.StreamFloor);
        switch (tower.CallBoss(player, canForce, out var needed))
        {
            case TowerManager.BossCallResult.Started:
                return; // the countdown is announced
            case TowerManager.BossCallResult.Voted:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_VOTE, tower.CurrentFloor, needed);
                return;
            case TowerManager.BossCallResult.NotOpen:
                client.SendReply(gm, tower.AllCleared ? Localization.MSG_TOWER_QUESTS_CLEARED : Localization.MSG_TOWER_BOSS_LOCKED, tower.StreamFloor, tower.QuestText());
                return;
            case TowerManager.BossCallResult.UnderWay:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_UNDER_WAY);
                return;
            case TowerManager.BossCallResult.Regrouping:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_REGROUPING, ((int)tower.RetrySecondsLeft).ToString(CultureInfo.InvariantCulture));
                return;
            case TowerManager.BossCallResult.NotInTower:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_ENTER_FIRST);
                return;
            default:
                client.SendReply(gm, Localization.MSG_TOWER_BOSS_NOBODY, tower.StreamFloor, floor != null ? floor.Boss.Name : "");
                return;
        }
    }

    private static void Info(TowerManager tower, GameMessage gm, GameClient client)
    {
        if (tower.State == null)
        {
            tower.RefreshState();
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var floor = Tower.Get(tower.StreamFloor);
        if (floor == null)
        {
            client.SendReply(gm, Localization.MSG_TOWER_NOT_READY);
            return;
        }
        var boss = tower.AllCleared ? Localization.MSG_TOWER_INFO_ALL_CLEARED
            : tower.State.BossOpen ? string.Format(Localization.MSG_TOWER_INFO_BOSS_OPEN, floor.Boss.Name)
            : string.Format(Localization.MSG_TOWER_INFO_BOSS_LOCKED, floor.Boss.Name);
        client.SendReply(gm, Localization.MSG_TOWER_INFO, floor.Number, Tower.TopFloor, floor.Name, floor.Level, tower.QuestText(), boss, tower.Players().Count);
    }
}

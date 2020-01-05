
public class IslandInfo : PacketHandler<Player>
{
    public IslandInfo(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendCommand(data.Username, "island_info", $"You're not in game. Use !join to start playing.");
            return;
        }

        var islandName = player.Island?.Identifier;
        if (string.IsNullOrEmpty(islandName))
        {
            if (player.Ferry.OnFerry)
            {
                var dest = player.Ferry.Destination;
                if (dest != null)
                {
                    client.SendCommand(data.Username, "island_info", $"You're currently on the ferry, going to disembark at '{dest.Identifier}'.");
                }
                else
                {
                    client.SendCommand(data.Username, "island_info", $"You're currently on the ferry.");
                }
            }
            else
            {
                var moderators = PlayerManager.GetAllModerators();
                var moderatorMessage = "";
                if (moderators.Count > 0)
                {
                    var moderator = moderators.Random();
                    moderatorMessage = $" {moderator.PlayerName}, could you please use !show @{player.PlayerName} to see if player is stuck?";
                }

                client.SendCommand(data.Username, "island_info", $"Uh oh. Your character may be stuck." + moderatorMessage);
            }
        }
        else
        {
            client.SendCommand(data.Username, "island_info", $"You're on the island called '{islandName}'");
        }
    }
}

public class TrainingInfo : PacketHandler<Player>
{
    public TrainingInfo(
       GameManager game,
       GameServer server,
       PlayerManager playerManager)
       : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (!player)
        {
            client.SendCommand(data.Username,
                "train_info", $"You're not in game. Use !join to start playing.");
            return;
        }

        var taskType = player.GetTask();
        if (taskType != TaskType.None)
        {
            string skill = "";
            if (taskType == TaskType.Fighting)
            {
                var args = player.GetTaskArguments();
                var skillIndex = player.GetCombatTypeFromArgs(args);
                if (skillIndex == 3)
                {
                    skill = "all";
                }
                else if (skillIndex >= 0)
                {
                    skill = player.GetCombatSkill(skillIndex)?.Name;
                }
            }
            else
            {
                skill = taskType.ToString();
            }

            if (!string.IsNullOrEmpty(skill))
            {
                client.SendCommand(data.Username,
                    "train_info",
                    $"You're currently training {skill}.");
                return;
            }
        }

        client.SendCommand(data.Username,
            "train_info",
            $"You're not training anything. Use !train <skill name> to start training!");
    }
}


public class PlayerLeave : PacketHandler<Player>
{
    public PlayerLeave(
    GameManager game,
    GameServer server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(Player data, GameClient client)
    {
        var player = PlayerManager.GetPlayer(data);
        if (player)
        {
            if (Game.Dungeons.JoinedDungeon(player))
            {
                Game.Dungeons.Remove(player);
            }

            if (!Game.Arena.CanJoin(player, out var joinedArena, out _) && joinedArena && Game.Arena.Started)
            {
                client.SendCommand(data.Username, "leave_failed", $"You cannot leave as you are participating in the arena that has already started and may break it. You have been queued up to leave after the arena has been finished.");
                Game.QueueRemovePlayer(player);
                return;
            }

            if (player.Duel.InDuel)
            {
                client.SendCommand(data.Username, "leave_failed", $"You cannot leave while fighting a duel. You have been queued up to leave after the duel has ended.");
                Game.QueueRemovePlayer(player);
                return;
            }

            Game.RemovePlayer(player);
            client.SendCommand(data.Username, "leave_success", $"You have left the game.");
        }
        else
        {
            client.SendCommand(data.Username, "leave_failed", $"You are not currently playing.");
        }
    }
}

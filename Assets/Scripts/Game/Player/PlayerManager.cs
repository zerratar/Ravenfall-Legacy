using Assets.Scripts;
using RavenNest.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Shinobytes.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RavenNest.SDK;

public class PlayerManager : MonoBehaviour
{
    private const string CacheDirectory = "data/";
    private const string CacheFileNameOld = "statcache.json";
    private const string CacheFileName = "data/statcache.bin";
    private const string CacheKey = "Ahgjkeaweg12!2KJAHgkhjeAhgegaeegjasdgauyEGIUM";

    //private readonly List<PlayerController> activePlayers = new List<PlayerController>();

    // Do not clear out this one. the rest of the dictionaries can be cleared. but this is for debugging purposes    
    private readonly Dictionary<string, string> userIdToNameLookup = new Dictionary<string, string>();

    private readonly Dictionary<string, PlayerController> platformIdLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<string, PlayerController> playerNameLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<Guid, PlayerController> playerIdLookup = new Dictionary<Guid, PlayerController>();
    private readonly Dictionary<Guid, PlayerController> userIdLookup = new Dictionary<Guid, PlayerController>();

    private readonly List<PlayerController> playerList = new List<PlayerController>();
    private readonly List<PlayerController> realPlayers = new List<PlayerController>();

    //private readonly object mutex = new object();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameSettings settings;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private IoCContainer ioc;

    //public readonly ConcurrentDictionary<Guid, Skills> StoredStats
    //    = new ConcurrentDictionary<Guid, Skills>();

    public readonly ConcurrentQueue<RavenNest.Models.Player> PlayerQueue
        = new ConcurrentQueue<RavenNest.Models.Player>();

    private DateTime lastCacheSave = DateTime.UnixEpoch;

    private ConcurrentQueue<Func<PlayerController>> addPlayerQueue = new ConcurrentQueue<Func<PlayerController>>();

    public bool LoadingPlayers;

    public PlayerController LastAddedPlayer;

    private void LateUpdate()
    {
        if (this.gameManager == null || this.gameManager.RavenNest == null || !this.gameManager.RavenNest.Authenticated || !this.gameManager.RavenNest.SessionStarted)
            return;

        if (addPlayerQueue.Count > 0)
        {
            LoadingPlayers = true;
            try
            {
                if (addPlayerQueue.TryDequeue(out var addPlayer))
                {
                    addPlayer();
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Failed to add player to game: " + exc);
            }
            if (addPlayerQueue.Count == 0)
            {
                gameManager.EndBatchedPlayerAdd();
            }
        }
        else { LoadingPlayers = false; }
    }

    void Start()
    {
        if (!gameManager) gameManager = GetComponent<GameManager>();
        if (!settings) settings = GetComponent<GameSettings>();
        if (!ioc) ioc = GetComponent<IoCContainer>();

        //LoadStatCache();
    }

    internal async Task<PlayerController> JoinAsync(GameMessage command, User user, GameClient client, bool userTriggered, bool isBot, Guid? characterId)
    {
        var Game = gameManager;
        try
        {
            if (user == null)
            {
                Shinobytes.Debug.LogError("A user tried to join the game but no user details available. Argument '" + nameof(user) + "' is null.");
                return null;
            }

            if (string.IsNullOrEmpty(user.PlatformId))
            {
                Shinobytes.Debug.LogError("A user tried to join the game but had no Id.");
                return null;
            }

            var addPlayerRequest = user;
            if (Game.RavenNest.SessionStarted)
            {
                if (!Game.Items.Loaded)
                {

                    if (userTriggered)
                    {
                        client.SendReplyUseMessageIfNotNull(command, user, Localization.GAME_NOT_LOADED);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Game not finished loading.");
                    return null;
                }
                if (Contains(addPlayerRequest.PlatformId, user.Platform) || Contains(user.Id))
                {
                    var alreadyInGameMessage = addPlayerRequest.Username + " failed to be added back to the game. Player is already in game.";
                    if (userTriggered)
                    {
                        client.SendReplyUseMessageIfNotNull(command, user, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                        Shinobytes.Debug.Log(alreadyInGameMessage);
                        return null;
                    }
                    else
                    {
                        // Try remove the player. this will replace current player with the new one if it has a characterId
                        // Note: this may be a potential bug later if you have one character in and then it get replaced by another.
                        //       the risk of this happening is extremely slim though.
                        var existingPlayer = GetPlayerByPlatformId(addPlayerRequest.PlatformId, addPlayerRequest.Platform) ?? GetPlayerByUserId(user.Id);
                        if (existingPlayer && (characterId == null || existingPlayer.Id == characterId))
                        {
                            Remove(existingPlayer);
                        }
                        else
                        {
                            Shinobytes.Debug.Log(alreadyInGameMessage);
                            return null;
                        }
                    }
                }
                var playerInfo = await Game.RavenNest.PlayerJoinAsync(
                    new RavenNest.Models.PlayerJoinData
                    {
                        Identifier = string.IsNullOrEmpty(addPlayerRequest.Identifier) ? "0" : addPlayerRequest.Identifier,
                        CharacterId = characterId ?? Guid.Empty,
                        Moderator = addPlayerRequest.IsModerator,
                        Subscriber = addPlayerRequest.IsSubscriber,
                        PlatformId = addPlayerRequest.PlatformId,
                        Platform = addPlayerRequest.Platform,
                        Vip = addPlayerRequest.IsVip,
                        UserId = addPlayerRequest.Id,
                        UserName = addPlayerRequest.Username,
                        IsGameRestore = !userTriggered
                    });
                if (playerInfo == null)
                {
                    if (userTriggered)
                    {
                        client.SendReplyUseMessageIfNotNull(command, addPlayerRequest, Localization.MSG_JOIN_FAILED);
                    }
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Missing PlayerInfo");
                    return null;
                }
                if (!playerInfo.Success)
                {
                    if (userTriggered)
                    {
                        client.SendReplyUseMessageIfNotNull(command, addPlayerRequest, playerInfo.ErrorMessage);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. " + playerInfo.ErrorMessage);
                    return null;
                }
                var player = AddPlayer(addPlayerRequest, playerInfo.Player, isBot);
                if (player == null || !player)
                {
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Player may already be in game.");
                    return null;
                }

                if (userTriggered && !player.IsBot)
                {
                    gameManager.SaveStateFile();

                    if (playerInfo.IsNewUser)
                    {
                        client.SendReply(command, player, Localization.MSG_JOIN_WELCOME_FIRST_TIME, addPlayerRequest.Username);
                    }
                    else
                    {
                        client.SendReply(command, player, Localization.MSG_JOIN_WELCOME);
                    }
                }
                return player;
            }
            else
            {
                if (userTriggered)
                    client.SendReplyUseMessageIfNotNull(command, user, Localization.GAME_NOT_READY);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("PlayerManager.JoinAsync: " + exc);
        }
        return null;
    }

    private PlayerController AddPlayer(User twitchUser, RavenNest.Models.Player playerInfo, bool isBot, bool isGameRestore = false)
    {
        var Game = gameManager;
        var player = Game.SpawnPlayer(playerInfo, twitchUser, isGameRestore: isGameRestore);
        if (player == null || !player)
        {
            return null;
        }

        player.Movement.Unlock(true);
        player.IsBot = isBot;
        if (player.IsBot)
        {
            player.Bot = this.gameObject.GetComponent<BotPlayerController>() ?? this.gameObject.AddComponent<BotPlayerController>();
            player.Bot.playerController = player;
            if (player.PlatformId != null && !player.PlatformId.StartsWith("#"))
            {
                player.PlatformId = "#" + player.PlatformId;
            }
        }

        player.PlayerNameHexColor = twitchUser.Color;
        LastAddedPlayer = player;
        // receiver:cmd|arg1|arg2|arg3|

        return player;
    }

    public bool Contains(Guid userId)
    {
        return GetPlayerByUserId(userId);
    }
    public bool Contains(string userId, string platform)
    {
        return GetPlayerByPlatformId(userId, platform);
    }
    public IReadOnlyList<PlayerController> GetAllBots()
    {
        return playerList.AsList(x => x.IsBot);
    }
    public IReadOnlyList<PlayerController> GetAllPlayers()
    {
        return playerList;
    }
    public IReadOnlyList<PlayerController> GetAllRealPlayers()
    {
        return realPlayers;
    }

    public PlayerController Spawn(
        Vector3 position,
        Player player,
        User user,
        StreamRaidInfo raidInfo,
        bool playerInitiatedJoin)
    {
        // if user is null, then we should use the player's connections instead. Pick the first one to decide which platform they come from.
        if (user == null || string.IsNullOrEmpty(user.PlatformId))
        {
            var platform = player.Connections.FirstOrDefault();
            if (platform != null)
            {
                user = new User(player, gameManager.RavenNest.UserId);
            }
            else
            {
                Shinobytes.Debug.LogWarning("Player '" + player.Name + "' not added to the game, missing platform details.");
                return null;
            }
        }

        var key = (user.Platform + "_" + user.PlatformId).ToLower();
        if (platformIdLookup.ContainsKey(key) || playerIdLookup.ContainsKey(player.Id))
        {
            Shinobytes.Debug.LogWarning("Player '" + player.Name + "' not added to the game, a player with same id or platform key exists. Key: '" + key + "'");
            return null;
        }

        var controller = Instantiate(playerControllerPrefab);
        if (!controller)
        {
            Shinobytes.Debug.LogError("Player Prefab not found!!!");
            return null;
        }

        controller.transform.position = position;

        return Add(controller.GetComponent<PlayerController>(), player, user, raidInfo, playerInitiatedJoin);
    }

    internal IReadOnlyList<PlayerController> GetAllModerators()
    {
        return platformIdLookup.Values.Where(x => x.IsModerator).ToList();
    }
    internal IReadOnlyList<PlayerController> GetAllGameAdmins()
    {
        return platformIdLookup.Values.Where(x => x.IsGameAdmin).ToList();
    }

    public PlayerController GetPlayer(User user)
    {
        var player = user.Id != Guid.Empty
            ? GetPlayerById(user.Id)
            : GetPlayerByPlatformId(user.PlatformId, user.Platform);

        if (!player) player = GetPlayerByName(user.Username);
        if (player)
        {
            player.UpdateUser(user);
        }
        return player;
    }


    public PlayerController GetPlayerByUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        if (userIdLookup.TryGetValue(userId, out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                RemoveLookup(plr);
                return null;
            }
            return plr;
        }

        return null;// playerTwitchIdLookup.Values.FirstOrDefault(x => x.Id.ToString().Equals(userId, StringComparison.InvariantCultureIgnoreCase));
    }

    public PlayerController GetPlayerByPlatformId(string userId, string platform)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        var key = (platform + "_" + userId).ToLower();
        if (platformIdLookup.TryGetValue(key, out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                platformIdLookup.Remove(key);

                RemoveLookup(plr);
                return null;
            }
            return plr;
        }

        return null;// playerTwitchIdLookup.Values.FirstOrDefault(x => x.Id.ToString().Equals(userId, StringComparison.InvariantCultureIgnoreCase));
    }

    public PlayerController GetPlayerByName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return null;

        playerName = playerName.StartsWith("@") ? playerName.Substring(1) : playerName;

        if (playerNameLookup.TryGetValue(playerName.ToLower(), out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                RemoveLookup(plr);
                return null;
            }
            return plr;
        }

        return null;
    }

    private void RemoveLookup(PlayerController plr)
    {
        var platformKey = (plr.Platform + "_" + plr.PlatformId).ToLower();
        platformIdLookup.Remove(platformKey);
        playerNameLookup.Remove(plr.Name.ToLower());
        playerIdLookup.Remove(plr.Id);
        userIdLookup.Remove(plr.UserId);
    }

    public int GetPlayerCount(bool includeNpc = false)
    {
        if (includeNpc)
        {
            return platformIdLookup.Count;
        }

        int count = 0;
        foreach (var item in platformIdLookup.Values)
        {
            if (!item.IsBot)
            {
                ++count;
            }
        }
        return count;
    }

    public PlayerController GetPlayerById(Guid characterId)
    {
        if (platformIdLookup == null)
        {
            return null;
        }

        if (playerIdLookup.TryGetValue(characterId, out var plr))
        {
            if (plr.isDestroyed || plr.Removed)
            {
                RemoveLookup(plr);
                return null;
            }
            return plr;
        }

        return playerList.FirstOrDefault(x => x.Id == characterId);
    }

    public PlayerController GetPlayerByIndex(int index)
    {
        if (playerList.Count <= index)
        {
            return null;
        }

        return playerList[index];
    }

    public void Remove(PlayerController player)
    {
        var platformKey = (player.Platform + "_" + player.PlatformId).ToLower();
        if (platformIdLookup.TryGetValue(platformKey, out var plrToRemove) || playerIdLookup.ContainsKey(player.Id))
        {
            gameManager.Village.TownHouses.InvalidateOwnership(player);
        }

        if (player)
        {
            RemoveLookup(player);
            playerList.Remove(player);
            realPlayers.Remove(player);

            player.OnRemoved();
            Destroy(player.gameObject);
        }
    }

    public IReadOnlyList<PlayerController> FindPlayers(string query)
    {
        return playerList.Where(x => x.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlayerController Add(
        PlayerController controller,
        Player player,
        User user,
        StreamRaidInfo raidInfo,
        bool playerInitiatedJoin)
    {
        controller.SetPlayer(player, user, raidInfo, gameManager, playerInitiatedJoin);

        var platformKey = (controller.Platform + "_" + controller.PlatformId).ToLower();
        platformIdLookup[platformKey] = controller;
        playerNameLookup[controller.PlayerName.ToLower()] = controller;
        playerIdLookup[controller.Id] = controller;
        userIdLookup[controller.UserId] = controller;
        playerList.Add(controller);

        if (TcpApi.IsValidPlayer(controller))
        {
            realPlayers.Add(controller);
        }

        gameManager.Village.TownHouses.InvalidateOwnership(controller);
        return controller;
    }

    internal void UpdateRestedState(RavenNest.Models.PlayerRestedUpdate data)
    {
        if (data == null) return;
        var player = GetPlayerById(data.PlayerId);
        if (player == null) return;
        player.SetRestedState(data);
    }

    internal async Task RestoreAsync(List<GameCachePlayerItem> players)
    {
        // even if players count is 0, do the request.
        // it will ensure the server later removes players not being recovered.
        //if (players.Count == 0) return;

        var failed = new List<GameCachePlayerItem>();
        try
        {
            Shinobytes.Debug.Log("Send Restore to server with " + players.Count + " players.");
            if (players.Count == 0)
            {
                await gameManager.RavenNest.Players.RestoreAsync(new PlayerRestoreData { Characters = new Guid[0] });
                return;
            }

            var id = players.Where(x => x != null).Select(x => x.CharacterId).ToArray();
            var result = await gameManager.RavenNest.Players.RestoreAsync(new PlayerRestoreData
            {
                Characters = id,
            });

            var i = 0;

            gameManager.BeginBatchedPlayerAdd();

            foreach (var playerInfo in result.Players)
            {
                if (playerInfo == null)
                {
                    continue;
                }

                var requested = players[i++];

                try
                {
                    if (!playerInfo.Success || playerInfo.Player == null)
                    {
                        failed.Add(requested);
                        Shinobytes.Debug.LogError("Failed to restore player (" + requested.User.Username + "): " + playerInfo.ErrorMessage);
                        continue;
                    }

                    addPlayerQueue.Enqueue(() =>
                    {
                        var p = AddPlayer(requested.User, playerInfo.Player, false, true);
                        if (p == null || !p)
                        {
                            return null;
                        }

                        p.LastChatCommandUtc = requested.LastActivityUtc;
                        if (p.LastChatCommandUtc <= DateTime.UnixEpoch)
                        {
                            p.LastChatCommandUtc = DateTime.UtcNow;
                        }

                        return p;
                    });

                    //var player = AddPlayer(false, requested.TwitchUser, playerInfo.Player);
                    //if (!player)
                    //{
                    //    failed.Add(requested);
                    //}
                }
                catch (Exception e)
                {
                    failed.Add(requested);
                }
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Unable to restore players: " + exc);
            gameManager.RavenBot.Announce("Failed to restore players. See game log files for more details.");
            return;
        }

        if (failed.Count > 0)
        {
            if (failed.Count > 10)
            {
                gameManager.RavenBot.Announce((players.Count - failed.Count) + " out of " + players.Count + " was added back to the game.");
            }
            else
            {
                gameManager.RavenBot.Announce(failed.Count + " players failed to be added back: " + String.Join(", ", failed.Select(x => x.User.Username)));
            }
        }
        else
        {

            gameManager.RavenBot.Announce(players.Count + " players restored.");
        }
    }


    //internal Skills GetStoredPlayerSkills(Guid id)
    //{
    //    if (StoredStats.TryGetValue(id, out var skills)) return skills;
    //    return null;
    //}
}

public class StatCacheData
{
    //public string UserId { get; set; }
    public Guid Id { get; set; }
    public Skills Skills { get; set; }
}


public static class StringCipher
{
    // This constant is used to determine the keysize of the encryption algorithm in bits.
    // We divide this by 8 within the code below to get the equivalent number of bytes.
    private const int Keysize = 256;

    // This constant determines the number of iterations for the password bytes generation function.
    private const int DerivationIterations = 1000;

    public static string Encrypt(string plainText, string passPhrase)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = Generate256BitsOfRandomEntropy();
        var ivStringBytes = Generate256BitsOfRandomEntropy();
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                            var cipherTextBytes = saltStringBytes;
                            cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                            cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                            memoryStream.Close();
                            cryptoStream.Close();
                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }
    }

    //public static string Decrypt(string cipherText, string passPhrase)
    //{
    //    // Get the complete stream of bytes that represent:
    //    // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
    //    var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
    //    // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
    //    var saltStringBytes = cipherTextBytesWithSaltAndIv.Slice(0, Keysize / 8).ToArray();
    //    // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
    //    var ivStringBytes = cipherTextBytesWithSaltAndIv.Slice(Keysize / 8, Keysize / 8).ToArray();
    //    // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
    //    var cipherTextBytes = cipherTextBytesWithSaltAndIv.Slice((Keysize / 8) * 2, cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

    //    using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
    //    {
    //        var keyBytes = password.GetBytes(Keysize / 8);
    //        using (var symmetricKey = new RijndaelManaged())
    //        {
    //            symmetricKey.BlockSize = 256;
    //            symmetricKey.Mode = CipherMode.CBC;
    //            symmetricKey.Padding = PaddingMode.PKCS7;
    //            using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
    //            {
    //                using (var memoryStream = new MemoryStream(cipherTextBytes))
    //                {
    //                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
    //                    {
    //                        var plainTextBytes = new byte[cipherTextBytes.Length];
    //                        var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
    //                        memoryStream.Close();
    //                        cryptoStream.Close();
    //                        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    private static byte[] Generate256BitsOfRandomEntropy()
    {
        var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
        using (var rngCsp = new RNGCryptoServiceProvider())
        {
            // Fill the array with cryptographically secure random bytes.
            rngCsp.GetBytes(randomBytes);
        }
        return randomBytes;
    }
}
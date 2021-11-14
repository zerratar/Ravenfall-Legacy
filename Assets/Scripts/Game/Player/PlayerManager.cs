using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private const string CacheDirectory = "data/";
    private const string CacheFileNameOld = "statcache.json";


    private const string CacheFileName = "data/statcache.bin";
    private const string CacheKey = "Ahgjkeaweg12!2KJAHgkhjeAhgegaeegjasdgauyEGIUM";

    //private readonly List<PlayerController> activePlayers = new List<PlayerController>();
    private readonly Dictionary<string, PlayerController> playerTwitchIdLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<string, PlayerController> playerNameLookup = new Dictionary<string, PlayerController>();
    private readonly Dictionary<Guid, PlayerController> playerIdLookup = new Dictionary<Guid, PlayerController>();

    private readonly List<PlayerController> playerList = new List<PlayerController>();

    //private readonly object mutex = new object();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameSettings settings;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private IoCContainer ioc;

    //public readonly ConcurrentDictionary<Guid, Skills> StoredStats
    //    = new ConcurrentDictionary<Guid, Skills>();

    public readonly ConcurrentQueue<RavenNest.Models.Player> PlayerQueue
        = new ConcurrentQueue<RavenNest.Models.Player>();

    private DateTime lastCacheSave = DateTime.MinValue;

    void Start()
    {
        if (!gameManager) gameManager = GetComponent<GameManager>();
        if (!settings) settings = GetComponent<GameSettings>();
        if (!ioc) ioc = GetComponent<IoCContainer>();

        //LoadStatCache();
    }

    internal async Task<PlayerController> JoinAsync(TwitchPlayerInfo data, GameClient client, bool userTriggered, bool isBot = false, Guid? characterId = null)
    {
        var Game = gameManager;
        try
        {

            if (string.IsNullOrEmpty(data.UserId))
            {
                Shinobytes.Debug.LogError("A user tried to join the game but had no UserId.");
                return null;
            }

            var addPlayerRequest = data;
            if (Game.RavenNest.SessionStarted)
            {
                if (!Game.Items.Loaded)
                {
                    if (userTriggered)
                    {
                        client.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_LOADED);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Game not finished loading.");
                    return null;
                }

                if (Contains(addPlayerRequest.UserId))
                {
                    if (userTriggered)
                    {
                        client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Player is already in game.");
                    return null;
                }

                //if (!isBot)
                //{
                //    Game.EventTriggerSystem.SendInput(addPlayerRequest.UserId, "join");
                //}

                var playerInfo = await Game.RavenNest.PlayerJoinAsync(
                    new RavenNest.Models.PlayerJoinData
                    {
                        Identifier = string.IsNullOrEmpty(addPlayerRequest.Identifier) ? "0" : addPlayerRequest.Identifier,
                        CharacterId = characterId ?? Guid.Empty,
                        Moderator = addPlayerRequest.IsModerator,
                        Subscriber = addPlayerRequest.IsSubscriber,
                        Vip = addPlayerRequest.IsVip,
                        UserId = addPlayerRequest.UserId,
                        UserName = addPlayerRequest.Username,
                        IsGameRestore = !userTriggered
                    });

                if (playerInfo == null)
                {
                    if (userTriggered)
                    {
                        client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED, addPlayerRequest.Username);
                    }
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Missing PlayerInfo");
                    return null;
                }

                if (!playerInfo.Success)
                {
                    if (userTriggered)
                    {
                        client.SendMessage(addPlayerRequest.Username, playerInfo.ErrorMessage);
                    }

                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. " + playerInfo.ErrorMessage);
                    return null;
                }

                var player = AddPlayer(isBot, addPlayerRequest, playerInfo.Player);
                if (player)
                {
                    if (userTriggered && !player.IsBot)
                    {
                        gameManager.SavePlayerStates();
                        client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_WELCOME);
                    }
                    return player;
                }
                else
                {
                    if (userTriggered)
                    {
                        client.SendMessage(addPlayerRequest.Username, Localization.MSG_JOIN_FAILED_ALREADY_PLAYING);
                    }
                    Shinobytes.Debug.LogError(addPlayerRequest.Username + " failed to be added back to the game. Player is already in game.");
                }
            }
            else
            {
                if (userTriggered)
                    client.SendMessage(addPlayerRequest.Username, Localization.GAME_NOT_READY);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError(exc);
        }
        return null;
    }

    private PlayerController AddPlayer(bool isBot, TwitchPlayerInfo twitchPlayerInfo, RavenNest.Models.Player ravenfallPlayerInfo)
    {
        var Game = gameManager;
        var player = Game.SpawnPlayer(ravenfallPlayerInfo, twitchPlayerInfo);
        if (player)
        {
            player.Unlock();
            player.IsBot = isBot;
            if (player.IsBot)
            {
                player.Bot = this.gameObject.AddComponent<BotPlayerController>();
                if (player.UserId != null && !player.UserId.StartsWith("#"))
                {
                    player.UserId = "#" + player.UserId;
                }
            }

            player.PlayerNameHexColor = twitchPlayerInfo.Color;
            //if (player.IsBroadcaster && !player.IsBot)
            //{
            //    Game.EventTriggerSystem.TriggerEvent("join", TimeSpan.FromSeconds(1));
            //}

            // receiver:cmd|arg1|arg2|arg3|
            return player;
        }
        return null;
    }

    //private void LoadStatCache()
    //{
    //    if (System.IO.File.Exists(CacheFileName))
    //    {
    //        var data = System.IO.File.ReadAllText(CacheFileName);
    //        var json = StringCipher.Decrypt(data, CacheKey);
    //        LoadStatCache(Newtonsoft.Json.JsonConvert.DeserializeObject<List<StatCacheData>>(json));
    //    }
    //}

    //private void LoadStatCache(List<StatCacheData> lists)
    //{
    //    foreach (var l in lists)
    //    {
    //        StoredStats[l.Id] = l.Skills;
    //    }
    //}

    //private void SaveStatCache()
    //{
    //    if (!System.IO.Directory.Exists(CacheDirectory))
    //        System.IO.Directory.CreateDirectory(CacheDirectory);

    //    var list = new List<StatCacheData>();
    //    foreach (var k in StoredStats.Keys)
    //    {
    //        list.Add(new StatCacheData
    //        {
    //            Id = k,
    //            Skills = StoredStats[k]
    //        });
    //    }

    //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
    //    var data = StringCipher.Encrypt(json, CacheKey);
    //    System.IO.File.WriteAllText(CacheFileName, data);
    //}

    public bool Contains(string userId)
    {
        return GetPlayerByUserId(userId);
    }

    //void Update()
    //{
    //    //var sinceLastSave = DateTime.UtcNow - lastCacheSave;
    //    //if (sinceLastSave >= TimeSpan.FromSeconds(10))
    //    //{
    //    //    SaveStatCache();
    //    //    lastCacheSave = DateTime.UtcNow;
    //    //}
    //}

    public IReadOnlyList<PlayerController> GetAllBots()
    {
        return playerList.Where(x => x.IsBot).ToList();
    }
    public IReadOnlyList<PlayerController> GetAllPlayers()
    {
        return playerList;
    }

    public PlayerController Spawn(
        Vector3 position,
        RavenNest.Models.Player playerDefinition,
        TwitchPlayerInfo twitchUser,
        StreamRaidInfo raidInfo)
    {

        if (playerTwitchIdLookup.ContainsKey(playerDefinition.UserId))
        {
            return null;
        }

        var player = Instantiate(playerControllerPrefab);
        if (!player)
        {
            Shinobytes.Debug.LogError("Player Prefab not found!!!");
            return null;
        }

        player.transform.position = position;

        return Add(player.GetComponent<PlayerController>(), playerDefinition, twitchUser, raidInfo);
    }

    internal IReadOnlyList<PlayerController> GetAllModerators()
    {
        return playerTwitchIdLookup.Values.Where(x => x.IsModerator).ToList();
    }

    public PlayerController GetPlayer(TwitchPlayerInfo twitchUser)
    {
        var player = GetPlayerByUserId(twitchUser.UserId);
        if (!player) player = GetPlayerByName(twitchUser.Username);
        if (player)
        {
            player.UpdateTwitchUser(twitchUser);
        }
        return player;
    }

    public PlayerController GetPlayerByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        if (playerTwitchIdLookup.TryGetValue(userId, out var user)) return user;

        return playerTwitchIdLookup.Values.FirstOrDefault(x => x.Id.ToString().Equals(userId, StringComparison.InvariantCultureIgnoreCase));
    }

    public PlayerController GetPlayerByName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return null;

        playerName = playerName.StartsWith("@") ? playerName.Substring(1) : playerName;

        if (playerNameLookup.TryGetValue(playerName.ToLower(), out var plr))
        {
            return plr;
        }

        return null;
    }

    public int GetPlayerCount(bool includeNpc = false)
    {
        return playerTwitchIdLookup.Values.Count(x => includeNpc || !x.IsNPC);
    }

    public PlayerController GetPlayerById(Guid characterId)
    {
        if (playerTwitchIdLookup == null)
        {
            return null;
        }

        if (playerIdLookup.TryGetValue(characterId, out var plr))
        {
            return plr;
        }

        return null;
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
        if (playerTwitchIdLookup.TryGetValue(player.UserId, out var plrToRemove))
        {
            playerTwitchIdLookup.Remove(player.UserId);
            playerNameLookup.Remove(player.PlayerName.ToLower());
            playerIdLookup.Remove(player.Id);
            playerList.Remove(plrToRemove);

            plrToRemove.OnRemoved();

            Destroy(plrToRemove.gameObject);
            gameManager.Village.TownHouses.InvalidateOwnershipOfHouses();
        }
    }

    public IReadOnlyList<PlayerController> FindPlayers(string query)
    {
        return playerList.Where(x => x.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlayerController Add(
        PlayerController player,
        RavenNest.Models.Player def,
        TwitchPlayerInfo twitchUser,
        StreamRaidInfo raidInfo)
    {
        player.SetPlayer(def, twitchUser, raidInfo);
        playerTwitchIdLookup[player.UserId] = player;
        playerNameLookup[player.PlayerName.ToLower()] = player;
        playerIdLookup[player.Id] = player;
        playerList.Add(player);

        gameManager.Village.TownHouses.InvalidateOwnershipOfHouses();
        return player;
    }

    internal void UpdateRestedState(RavenNest.Models.PlayerRestedUpdate data)
    {
        if (data == null) return;
        var player = GetPlayerById(data.CharacterId);
        if (player == null) return;
        player.SetRestedState(data);
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

    public static string Decrypt(string cipherText, string passPhrase)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
        // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
        var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

        using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
        {
            var keyBytes = password.GetBytes(Keysize / 8);
            using (var symmetricKey = new RijndaelManaged())
            {
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                {
                    using (var memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            var plainTextBytes = new byte[cipherTextBytes.Length];
                            var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                            memoryStream.Close();
                            cryptoStream.Close();
                            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                        }
                    }
                }
            }
        }
    }

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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private const string CacheDirectory = "data/";
    private const string CacheFileNameOld = "statcache.json";
    private const string CacheFileName = "data/statcache.bin";
    private const string CacheKey = "Ahgjkeaweg12!2KJAHgkhjeAhgegaeegjasdgauyEGIUM";

    private readonly List<PlayerController> activePlayers = new List<PlayerController>();
    private readonly object mutex = new object();

    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameSettings settings;
    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private IoCContainer ioc;

    public readonly ConcurrentDictionary<string, Skills> StoredStats
        = new ConcurrentDictionary<string, Skills>();

    public readonly ConcurrentQueue<RavenNest.Models.Player> PlayerQueue
        = new ConcurrentQueue<RavenNest.Models.Player>();

    private DateTime lastCacheSave = DateTime.MinValue;


    void Start()
    {
        if (!gameManager) gameManager = GetComponent<GameManager>();
        if (!settings) settings = GetComponent<GameSettings>();
        if (!ioc) ioc = GetComponent<IoCContainer>();
        LoadStatCache();
    }

    private void LoadStatCache()
    {
        if (System.IO.File.Exists(CacheFileNameOld) && Application.version == "0.4.3a")
        {
            var json = System.IO.File.ReadAllText(CacheFileNameOld);
            LoadStatCache(Newtonsoft.Json.JsonConvert.DeserializeObject<List<StatCacheData>>(json));
            System.IO.File.Delete(CacheFileNameOld);
            return;
        }

        if (System.IO.File.Exists(CacheFileName))
        {
            var data = System.IO.File.ReadAllText(CacheFileName);
            var json = StringCipher.Decrypt(data, CacheKey);
            LoadStatCache(Newtonsoft.Json.JsonConvert.DeserializeObject<List<StatCacheData>>(json));
        }
    }

    private void LoadStatCache(List<StatCacheData> lists)
    {
        foreach (var l in lists)
        {
            StoredStats[l.UserId] = l.Skills;
        }
    }

    private void SaveStatCache()
    {
        if (!System.IO.Directory.Exists(CacheDirectory))
            System.IO.Directory.CreateDirectory(CacheDirectory);

        var list = new List<StatCacheData>();
        foreach (var k in StoredStats.Keys)
        {
            list.Add(new StatCacheData { UserId = k, Skills = StoredStats[k] });
        }

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
        var data = StringCipher.Encrypt(json, CacheKey);
        System.IO.File.WriteAllText(CacheFileName, data);
    }

    public bool Contains(string userId)
    {
        return GetPlayerByUserId(userId);
    }

    void Update()
    {
        var sinceLastSave = DateTime.UtcNow - lastCacheSave;
        if (sinceLastSave >= TimeSpan.FromSeconds(10))
        {
            SaveStatCache();
            lastCacheSave = DateTime.UtcNow;
        }
    }

    public IReadOnlyList<PlayerController> GetAllPlayers()
    {
        return activePlayers;
    }

    public PlayerController Spawn(
        Vector3 position,
        RavenNest.Models.Player playerDefinition,
        Player streamUser,
        StreamRaidInfo raidInfo)
    {
        if (activePlayers.Any(x => x.PlayerName == playerDefinition.Name))
        {
            return null; // player is already in game
        }

        var player = Instantiate(playerControllerPrefab);
        if (!player)
        {
            Debug.LogError("Player Prefab not found!!!");
            return null;
        }

        player.transform.position = position;

        return Add(player.GetComponent<PlayerController>(), playerDefinition, streamUser, raidInfo);
    }

    internal IReadOnlyList<PlayerController> GetAllModerators()
    {
        return activePlayers.Where(x => x.IsModerator).ToList();
    }

    public PlayerController GetPlayer(Player taskPlayer)
    {
        var player = GetPlayerByName(taskPlayer.Username);
        return player ? player : GetPlayerByUserId(taskPlayer.UserId);
    }

    public PlayerController GetPlayerByUserId(string userId)
    {
        lock (mutex)
        {
            return activePlayers.FirstOrDefault(x =>
                x.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public PlayerController GetPlayerByName(string playerName)
    {
        playerName = playerName.StartsWith("@") ? playerName.Substring(1) : playerName;
        lock (mutex)
        {
            return activePlayers.FirstOrDefault(x =>
            x.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    public int GetPlayerCount(bool includeNpc = false)
    {
        lock (mutex)
            return activePlayers?.Count(x => includeNpc || !x.IsNPC) ?? 0;
    }

    public PlayerController GetPlayerByIndex(int index)
    {
        lock (mutex)
        {
            if (activePlayers == null || activePlayers.Count <= index)
            {
                return null;
            }

            return activePlayers[index];
        }
    }

    public void Remove(PlayerController player)
    {
        lock (mutex)
        {
            if (!activePlayers.Contains(player))
            {
                return;
            }

            player.OnRemoved();
            activePlayers.Remove(player);
            Destroy(player.gameObject);
            gameManager.Village.TownHouses.InvalidateOwnershipOfHouses();
        }
    }

    public IReadOnlyList<PlayerController> FindPlayers(string query)
    {
        lock (mutex)
            return activePlayers.Where(x => x.PlayerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlayerController Add(
        PlayerController player,
        RavenNest.Models.Player def,
        Player streamUser,
        StreamRaidInfo raidInfo)
    {
        player.SetPlayer(def, streamUser, raidInfo);
        lock (mutex)
        {
            activePlayers.Add(player);
            StoredStats[player.UserId] = player.Stats;
            gameManager.Village.TownHouses.InvalidateOwnershipOfHouses();
            return player;
        }
    }

    internal Skills GetStoredPlayerSkills(string userId)
    {
        if (StoredStats.TryGetValue(userId, out var skills)) return skills;
        return null;
    }
}

public class StatCacheData
{
    public string UserId { get; set; }
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
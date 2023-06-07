using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

namespace RavenNest.SDK
{
    public class RavenNestClient : IDisposable
    {
        public readonly IAppSettings Settings;
        private readonly ILogger logger;
        private readonly ITokenProvider tokenProvider;

        private readonly GameManager gameManager;
        private AuthToken currentAuthToken;
        private SessionToken currentSessionToken;

        private int activeRequestCount;
        private int badClientVersion;

        private readonly BotPlayerGenerator botPlayerGenerator;
        public bool BadClientVersion => Volatile.Read(ref badClientVersion) == 1;

        private readonly ConcurrentQueue<LoyaltyUpdate> loyaltyUpdateQueue = new();

        private Thread thread;
        private bool disposed;
        internal bool AwaitingSessionStart;
        private float SessionStartTime;
        private Task<bool> startSessionTask;

        public RavenNestClient(
            ILogger logger,
            GameManager gameManager)
        {

            Settings =
                        new ProductionEndpoint()
                        //new StagingRavenNestStreamSettings()
                        //new LocalServerRemoteBotEndpoint()
                        //new DevServerRemoteBotEndpoint()
                        //new LocalEndpoint()
                        ;

            ServicePointManager.DefaultConnectionLimit = 2000;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateCertificate);
            //ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy();

            this.logger = logger ?? new UnityLogger();
            this.gameManager = gameManager;
            var binarySerializer = new CompressedJsonSerializer();//new BinarySerializer();

            tokenProvider = new TokenProvider();
            var request = new WebApiRequestBuilderProvider(Settings, tokenProvider);

            Tcp = new TcpApi(gameManager, Settings.TcpApiEndpoint, tokenProvider);

            Auth = new AuthApi(this, logger, request);
            Game = new GameApi(this, logger, request);
            Items = new ItemsApi(this, logger, request);
            Players = new PlayersApi(this, logger, request);
            Marketplace = new MarketplaceApi(this, logger, request);
            Village = new VillageApi(this, logger, request);
            Clan = new ClanApi(this, logger, request);
            botPlayerGenerator = new BotPlayerGenerator();

            thread = new System.Threading.Thread(UpdateProcess);
            thread.Start();
        }

        public TcpApi Tcp { get; }
        public AuthApi Auth { get; }
        public GameApi Game { get; }
        public ItemsApi Items { get; }
        public PlayersApi Players { get; }
        public MarketplaceApi Marketplace { get; }

        public ClanApi Clan { get; }
        public VillageApi Village { get; }


        public bool Authenticated => currentAuthToken != null &&
                                       currentAuthToken.UserId != Guid.Empty &&
                                       !currentAuthToken.Expired;

        public bool SessionStarted => currentSessionToken != null &&
                                      !string.IsNullOrEmpty(currentSessionToken.AuthToken) &&
                                      !currentSessionToken.Expired;

        public bool HasActiveRequest => activeRequestCount > 0;

        public string ServerAddress => Settings.WebApiEndpoint;
        public Guid SessionId => currentSessionToken?.SessionId ?? Guid.Empty;
        public Guid UserId => currentSessionToken?.UserId ?? Guid.Empty;

        public Dictionary<string, object> UserSettings { get; private set; }


        [Obsolete] public string TwitchUserName { get; private set; }
        [Obsolete] public string TwitchDisplayName { get; private set; }
        [Obsolete] public string TwitchUserId { get; private set; }

        public void EnqueueLoyaltyUpdate(CheerBitsEvent data)
        {
            loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
            {
                BitsCount = data.Bits,
                UserId = data.UserId,
                UserName = data.UserName,
                Date = DateTime.UtcNow
            });
        }

        public void EnqueueLoyaltyUpdate(UserSubscriptionEvent data)
        {
            loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
            {
                SubsCount = 1,
                UserId = data.UserId,
                UserName = data.UserName,
                Date = DateTime.UtcNow
            });
        }

        private async void UpdateProcess()
        {
            while (!disposed)
            {
                if (startSessionTask != null)
                {
                    await startSessionTask;
                    startSessionTask = null;
                }

                if (Authenticated && SessionStarted)
                {
                    var failedRequest = false;
                    try
                    {
                        if (loyaltyUpdateQueue.TryDequeue(out var req))
                        {
                            if (!await (Players.SendLoyaltyUpdateAsync(req).ConfigureAwait(false)))
                            {
                                failedRequest = true;
                                loyaltyUpdateQueue.Enqueue(req);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.WriteError("Failed to send loyalty data to server: " + exc);
                    }
                    finally
                    {
                    }
                    Thread.Sleep(failedRequest ? 200 : 16);
                    continue;
                }

                System.Threading.Thread.Sleep(200);
            }
        }

        internal void SavePlayerExperience(IReadOnlyList<PlayerController> players, bool saveAllSkills = false)
        {
            Tcp.SavePlayerExperience(players, saveAllSkills);
        }

        internal void SavePlayerState(IReadOnlyList<PlayerController> players)
        {
            Tcp.SavePlayerState(players);
        }


        public bool SavePlayer(PlayerController player, PlayerUpdateType updateType)
        {
            if (!player || player == null || !SessionStarted || !Tcp.IsReady)
            {
                return false;
            }

            if (player.IsBot)
            {
                return true;
            }

            if (!TcpApi.IsValidPlayer(player))
            {
                return true;
            }

            Tcp.UpdatePlayer(player, updateType);
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                var authToken = await Auth.AuthenticateAsync(username, password);
                if (authToken != null)
                {
                    currentAuthToken = authToken;
                    tokenProvider.SetAuthToken(currentAuthToken);
                    gameManager.OnAuthenticated();
                    return true;
                }
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);

            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
            return false;
        }

        public async Task<bool> StartSessionAsync(string clientVersion, string accessKey)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                var result = await Game.BeginSessionAsync(clientVersion, accessKey, SessionStartTime);

                if (result == null || result.State != BeginSessionResultState.Success || result.SessionToken == null)
                {
                    Interlocked.CompareExchange(ref badClientVersion, 1, 0);
                    await Task.Delay(250);
                    return false;
                }

                tokenProvider.SetSessionToken(result.SessionToken);
                currentSessionToken = result.SessionToken;
                TwitchUserName = currentSessionToken.TwitchUserName;
                TwitchDisplayName = currentSessionToken.TwitchDisplayName;
                TwitchUserId = currentSessionToken.TwitchUserId;

                UserSettings = result.UserSettings;

                AwaitingSessionStart = false;
                gameManager.OnSessionStart();
                gameManager.HandleGameEvent(result.Village);
                gameManager.HandleGameEvent(result.Permissions);
                gameManager.HandleGameEvent(result.ExpMultiplier);
                return true;
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
            return false;
        }

        internal async void PlayerRemoveAsync(PlayerController player)
        {
            try
            {
                if (player.IsBot && player.PlatformId.StartsWith("#"))
                {
                    return;
                }

                Interlocked.Increment(ref activeRequestCount);
                await Players.PlayerRemoveAsync(player.Id);
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
        }

        public async Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData joinData, int retry = 0)
        {
            try
            {
                // way to fake a bot here.
                if (joinData.UserId != null && joinData.PlatformId.StartsWith("#"))
                {
                    return botPlayerGenerator.Generate(joinData);
                }

                if (retry >= 5)
                {
                    logger.WriteError("Unable to add the player: " + joinData.UserName + ", tried " + (retry) + " times.");
                    return null;
                }

                Interlocked.Increment(ref activeRequestCount);

                var playerResult = await Players.PlayerJoinAsync(joinData);

                //#if DEBUG
                if (retry > 0 && playerResult.Success)
                {
                    logger.WriteDebug(joinData.UserName + " was successfully added to the game after " + retry + " tries.");
                }
                //#endif
                return playerResult;
            }
            catch (Exception exc)
            {
                logger.WriteDebug("Failed to add player (" + joinData.UserName + "). " + exc.Message + ". retrying (Try: " + (retry + 1) + ")...");

                if (exc is System.Net.WebException)
                {
                    await Task.Delay(250 * (retry + 1));
                    return await PlayerJoinAsync(joinData, retry + 1);
                }

                return null;
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
        }

        public async Task<bool> EndSessionAndRaidAsync(string username, bool war)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                return await Game.EndSessionAndRaidAsync(username, war);
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
                currentSessionToken = null;
                tokenProvider.SetSessionToken(null);
            }
        }

        public async Task<bool> EndSessionAsync()
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                await Game.EndSessionAsync();
                return true;
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
                currentSessionToken = null;
                tokenProvider.SetSessionToken(null);
            }
        }
        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public Task<bool> ClearLogoAsync(string twitchUserId)
        {
            return this.Game.ClearLogoAsync(twitchUserId);
        }

        internal async void Terminate()
        {
            Dispose();
            await EndSessionAsync();
        }

        public void Dispose()
        {
            try
            {
                Tcp.Dispose();
            }
            catch { }
            disposed = true;
        }

        internal void StartSession(string version, string accessKey)
        {
            AwaitingSessionStart = true;
            SessionStartTime = UnityEngine.Time.time;
            startSessionTask = StartSessionAsync(version, accessKey);
        }
    }
}
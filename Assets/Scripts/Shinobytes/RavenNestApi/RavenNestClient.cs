using System;
using System.Collections.Concurrent;
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
        private readonly ILogger logger;
        private readonly IAppSettings appSettings;
        private readonly ITokenProvider tokenProvider;

        private readonly GameManager gameManager;
        private AuthToken currentAuthToken;
        private SessionToken currentSessionToken;

        private int activeRequestCount;
        private int updateCounter;
        private int revision;
        private int badClientVersion;

        private readonly BotPlayerGenerator botPlayerGenerator;
        public bool BadClientVersion => Volatile.Read(ref badClientVersion) == 1;

        private readonly ConcurrentQueue<LoyaltyUpdate> loyaltyUpdateQueue = new();
        private readonly ConcurrentQueue<Func<Task>> pendingAsyncRequests = new();

        private Thread thread;
        private bool terminated;
        private bool disposed;
        internal bool AwaitingSessionStart;
        private float SessionStartTime;

        public RavenNestClient(
            ILogger logger,
            GameManager gameManager,
            IAppSettings settings)
        {
            ServicePointManager.DefaultConnectionLimit = 2000;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateCertificate);
            //ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy();

            this.logger = logger ?? new UnityLogger();
            this.gameManager = gameManager;
            var binarySerializer = new CompressedJsonSerializer();//new BinarySerializer();
            appSettings = settings ?? new ProductionRavenNestStreamSettings();

            tokenProvider = new TokenProvider();
            var request = new WebApiRequestBuilderProvider(appSettings, tokenProvider);

            WebSocket = new WebSocketApi(this, gameManager, logger, settings, tokenProvider, new GamePacketSerializer(binarySerializer));
            Tcp = new TcpApi(gameManager, appSettings.TcpApiEndpoint, tokenProvider);

            Auth = new AuthApi(this, logger, request);
            Game = new GameApi(this, logger, request);
            Items = new ItemsApi(this, logger, request);
            Players = new PlayersApi(this, logger, request);
            Marketplace = new MarketplaceApi(this, logger, request);
            Village = new VillageApi(this, logger, request);
            Clan = new ClanApi(this, logger, request);
            botPlayerGenerator = new BotPlayerGenerator();

            thread = new System.Threading.Thread(UpdateThread);
            thread.Start();
        }

        public WebSocketApi WebSocket { get; }
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

        public string ServerAddress => appSettings.WebApiEndpoint;
        public Guid SessionId => currentSessionToken?.SessionId ?? Guid.Empty;
        public string TwitchUserName { get; private set; }
        public string TwitchDisplayName { get; private set; }
        public string TwitchUserId { get; private set; }

        public void EnqueueLoyaltyUpdate(TwitchCheer data)
        {
            loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
            {
                BitsCount = data.Bits,
                UserId = data.UserId,
                UserName = data.UserName,
                Date = DateTime.UtcNow
            });
        }

        public void EnqueueLoyaltyUpdate(TwitchSubscription data)
        {
            loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
            {
                SubsCount = 1,
                UserId = data.UserId,
                UserName = data.UserName,
                Date = DateTime.UtcNow
            });
        }

        private async void UpdateThread()
        {
            while (!disposed)
            {
                if (!await WebSocket.UpdateAsync())
                {
                    logger.Debug("Reconnecting to server...");
                    await Task.Delay(200);
                    continue;
                }

                if (pendingAsyncRequests.TryDequeue(out var pendingAsyncRequest))
                {
                    await pendingAsyncRequest();
                }

                if (Authenticated && SessionStarted)
                {
                    try
                    {
                        if (loyaltyUpdateQueue.TryDequeue(out var req))
                        {
                            if (!await Players.SendLoyaltyUpdateAsync(req))
                            {
                                loyaltyUpdateQueue.Enqueue(req);
                                await Task.Delay(2000);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.Error("Failed to send loyalty data to server: " + exc);
                    }
                    finally
                    {
                    }
                }
                await Task.Delay(16);
            }
        }

        public bool SaveTrainingSkill(PlayerController player)
        {
            if (!player || player == null)
            {
                return false;
            }

            if (player.IsBot)
            {
                return true;
            }

            // if we are connected to the Tcp Api, we will use that instead.
            if (Tcp.Connected)
            {
                Tcp.UpdatePlayer(player);
                return true;
            }

            var saveResult = WebSocket.SaveActiveSkill(player);
            WebSocket.SavePlayerState(player);
            return saveResult;
        }

        public bool SavePlayer(PlayerController player)
        {
            if (!player || player == null)
            {
                return false;
            }

            if (player.IsBot)
            {
                return true;
            }

            if (!SessionStarted)
            {
                //Shinobytes.Debug.Log("Trying to save player " + player.PlayerName + " but session has not been started.");
                return false;
            }

            // if we are connected to the Tcp Api, we will use that instead.
            if (Tcp.Connected)
            {
                Tcp.UpdatePlayer(player);
                return true;
            }


            var saveResult = WebSocket.SavePlayerSkills(player);
            WebSocket.SavePlayerState(player);
            return saveResult;
        }

        //public async Task<bool> SavePlayerStateAsync(PlayerController player)
        //{
        //    if (!player || player == null)
        //    {
        //        return false;
        //    }

        //    if (player.IsBot && player.UserId.StartsWith("#"))
        //    {
        //        return true;
        //    }

        //    if (!SessionStarted)
        //    {
        //        //Shinobytes.Debug.Log("Trying to save player " + player.PlayerName + " but session has not been started.");
        //        return false;
        //    }

        //    return Stream.SavePlayerState(player);
        //}

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
                logger.Error(exc.Message);

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
                AwaitingSessionStart = false;
                gameManager.OnSessionStart();
                gameManager.HandleGameEvent(result.Village);
                gameManager.HandleGameEvent(result.Permissions);
                gameManager.HandleGameEvent(result.ExpMultiplier);
                return true;
            }
            catch (Exception exc)
            {
                logger.Error(exc.Message);
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
                if (player.IsBot && player.UserId.StartsWith("#"))
                {
                    return;
                }

                Interlocked.Increment(ref activeRequestCount);
                await Players.PlayerRemoveAsync(player.Id);
            }
            catch (Exception exc)
            {
                logger.Error(exc.Message);
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
                if (joinData.UserId != null && joinData.UserId.StartsWith("#"))
                {
                    return botPlayerGenerator.Generate(joinData);
                }

                if (retry >= 5)
                {
                    logger.Error("Unable to add the player: " + joinData.UserName + ", tried " + (retry) + " times.");
                    return null;
                }

                Interlocked.Increment(ref activeRequestCount);

                var playerResult = await Players.PlayerJoinAsync(joinData);

                //#if DEBUG
                if (retry > 0 && playerResult.Success)
                {
                    logger.Debug(joinData.UserName + " was successfully added to the game after " + retry + " tries.");
                }
                //#endif
                return playerResult;
            }
            catch (Exception exc)
            {
                logger.Debug("Failed to add player (" + joinData.UserName + "). " + exc.Message + ". retrying (Try: " + (retry + 1) + ")...");

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
                logger.Error(exc.Message);
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
                logger.Error(exc.Message);
                return false;
            }
            finally
            {
                WebSocket.Close();
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
            this.terminated = true;
        }

        public void Dispose()
        {
            try
            {
                Tcp.Dispose();
            }
            catch { }
            try
            {
                WebSocket.Close();
            }
            catch { }
            disposed = true;
        }

        internal void StartSession(string version, string accessKey)
        {
            AwaitingSessionStart = true;
            SessionStartTime = UnityEngine.Time.time;
            pendingAsyncRequests.Enqueue(() => StartSessionAsync(version, accessKey));
        }
    }
}
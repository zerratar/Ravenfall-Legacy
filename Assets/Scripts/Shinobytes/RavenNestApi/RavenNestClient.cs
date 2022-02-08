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

    public class RavenNestClient : IRavenNestClient
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

        private readonly ConcurrentQueue<LoyaltyUpdate> loyaltyUpdateQueue
            = new ConcurrentQueue<LoyaltyUpdate>();

        private SemaphoreSlim loyaltyUpdateLock = new SemaphoreSlim(1);

        public RavenNestClient(
            ILogger logger,
            GameManager gameManager,
            IAppSettings settings)
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateCertificate);
            //ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy();

            this.logger = logger ?? new UnityLogger();
            this.gameManager = gameManager;
            var binarySerializer = new CompressedJsonSerializer();//new BinarySerializer();
            appSettings = settings ?? new ProductionRavenNestStreamSettings();

            tokenProvider = new TokenProvider();
            var request = new WebApiRequestBuilderProvider(appSettings, tokenProvider);

            Stream = new WebSocketEndpoint(this, gameManager, logger, settings, tokenProvider, new GamePacketSerializer(binarySerializer));
            Auth = new WebBasedAuthEndpoint(this, logger, request);
            Game = new WebBasedGameEndpoint(this, logger, request);
            Items = new WebBasedItemsEndpoint(this, logger, request);
            Players = new WebBasedPlayersEndpoint(this, logger, request);
            Marketplace = new WebBasedMarketplaceEndpoint(this, logger, request);
            Village = new WebBasedVillageEndpoint(this, logger, request);

            botPlayerGenerator = new BotPlayerGenerator();
        }

        public IWebSocketEndpoint Stream { get; }
        public IAuthEndpoint Auth { get; }
        public IGameEndpoint Game { get; }
        public IItemEndpoint Items { get; }
        public IPlayerEndpoint Players { get; }
        public IMarketplaceEndpoint Marketplace { get; }

        public IVillageEndpoint Village { get; }


        public bool Authenticated => currentAuthToken != null &&
                                       currentAuthToken.UserId != Guid.Empty &&
                                       !currentAuthToken.Expired;

        public bool SessionStarted => currentSessionToken != null &&
                                      !string.IsNullOrEmpty(currentSessionToken.AuthToken) &&
                                      !currentSessionToken.Expired;

        public bool HasActiveRequest => activeRequestCount > 0;

        public string ServerAddress => appSettings.ApiEndpoint;
        public Guid SessionId { get; private set; }
        public string TwitchUserName { get; private set; }
        public string TwitchDisplayName { get; private set; }
        public string TwitchUserId { get; private set; }

        public void EnqueueLoyaltyUpdate(TwitchCheer data)
        {
            loyaltyUpdateLock.Wait();

            loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
            {
                BitsCount = data.Bits,
                UserId = data.UserId,
                UserName = data.UserName,
                Date = DateTime.UtcNow
            });

            loyaltyUpdateLock.Release();
        }

        public void EnqueueLoyaltyUpdate(TwitchSubscription data)
        {
            loyaltyUpdateLock.Wait();

            var items = loyaltyUpdateQueue.ToArray();
            var existingSubGift = items.FirstOrDefault(x => x.UserId == data.UserId && x.SubsCount > 0);
            if (existingSubGift != null)
            {
                existingSubGift.SubsCount++;

                loyaltyUpdateQueue.Clear();
                foreach (var item in items)
                {
                    loyaltyUpdateQueue.Enqueue(item);
                }
            }
            else
            {
                loyaltyUpdateQueue.Enqueue(new LoyaltyUpdate
                {
                    SubsCount = 1,
                    UserId = data.UserId,
                    UserName = data.UserName,
                    Date = DateTime.UtcNow
                });
            }

            loyaltyUpdateLock.Release();
        }

        public async void Update()
        {
            if (Interlocked.CompareExchange(ref updateCounter, 1, 0) == 1)
            {
                return;
            }

            if (!await Stream.UpdateAsync())
            {
                logger.Debug("Reconnecting to server...");
            }

            if (Authenticated && SessionStarted)
            {
                try
                {
                    await loyaltyUpdateLock.WaitAsync();

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
                    logger.Error("Failed to send loyualty data to server: " + exc);
                }
                finally
                {
                    loyaltyUpdateLock.Release();
                }
            }

            Interlocked.Decrement(ref updateCounter);
        }

        public Task<bool> SaveTrainingSkill(PlayerController player)
        {
            if (!player || player == null)
            {
                return Task.FromResult(false);
            }

            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return Task.FromResult(true);
            }

            return Stream.SaveActiveSkillAsync(player);
        }

        public async Task<bool> SavePlayerAsync(PlayerController player)
        {
            if (!player || player == null)
            {
                return false;
            }

            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return true;
            }

            if (!SessionStarted)
            {
                //Shinobytes.Debug.Log("Trying to save player " + player.PlayerName + " but session has not been started.");
                return false;
            }

            var saveResult = await Stream.SavePlayerSkillsAsync(player);
            await Stream.SavePlayerStateAsync(player);
            return saveResult;
        }

        public async Task<bool> SavePlayerStateAsync(PlayerController player)
        {
            if (!player || player == null)
            {
                return false;
            }

            if (player.IsBot && player.UserId.StartsWith("#"))
            {
                return true;
            }

            if (!SessionStarted)
            {
                //Shinobytes.Debug.Log("Trying to save player " + player.PlayerName + " but session has not been started.");
                return false;
            }

            return await Stream.SavePlayerStateAsync(player);
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
                logger.Error(exc.Message);

            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
            return false;
        }

        public async Task<bool> StartSessionAsync(string clientVersion, string accessKey, bool useLocalPlayers)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                var sessionToken = await Game.BeginSessionAsync(clientVersion, accessKey, useLocalPlayers, UnityEngine.Time.time);
                if (sessionToken != null)
                {
                    tokenProvider.SetSessionToken(sessionToken);
                    currentSessionToken = sessionToken;
                    SessionId = currentSessionToken.SessionId;
                    TwitchUserName = currentSessionToken.TwitchUserName;
                    TwitchDisplayName = currentSessionToken.TwitchDisplayName;
                    TwitchUserId = currentSessionToken.TwitchUserId;
                    gameManager.OnSessionStart();
                    return true;
                }
                else
                {
                    Interlocked.CompareExchange(ref badClientVersion, 1, 0);
                }

                await Task.Delay(250);
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
                Stream.Close();
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
    }
}
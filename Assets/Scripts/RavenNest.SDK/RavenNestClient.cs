using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RavenNest.Models;
using RavenNest.SDK.Endpoints;

namespace RavenNest.SDK
{
    public class RavenNestClient : IRavenNestClient
    {
        //private readonly TimeSpan tokenRefreshInterval = TimeSpan.FromHours(1);

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

        //private string username;
        //private string password;
        private DateTime lastLogin;

        public bool BadClientVersion => Volatile.Read(ref badClientVersion) == 1;

        public RavenNestClient(
            ILogger logger,
            GameManager gameManager,
            IAppSettings settings)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy();

            this.logger = logger ?? new UnityLogger();
            this.gameManager = gameManager;
            var binarySerializer = new BinarySerializer();
            appSettings = settings ?? new RavenNestStreamSettings();

            tokenProvider = new TokenProvider();
            var request = new WebApiRequestBuilderProvider(appSettings, tokenProvider);

            Stream = new WebSocketEndpoint(gameManager, logger, settings, tokenProvider, new GamePacketSerializer(binarySerializer));
            Auth = new WebBasedAuthEndpoint(this, logger, request);
            Game = new WebBasedGameEndpoint(this, logger, request);
            Items = new WebBasedItemsEndpoint(this, logger, request);
            Players = new WebBasedPlayersEndpoint(this, logger, request);
            Marketplace = new WebBasedMarketplaceEndpoint(this, logger, request);
        }

        public IWebSocketEndpoint Stream { get; }
        public IAuthEndpoint Auth { get; }
        public IGameEndpoint Game { get; }
        public IItemEndpoint Items { get; }
        public IPlayerEndpoint Players { get; }
        public IMarketplaceEndpoint Marketplace { get; }

        public bool Authenticated => currentAuthToken != null &&
                                       currentAuthToken.UserId != Guid.Empty &&
                                       !currentAuthToken.Expired;

        public bool SessionStarted => currentSessionToken != null &&
                                      !string.IsNullOrEmpty(currentSessionToken.AuthToken) &&
                                      !currentSessionToken.Expired;

        public bool HasActiveRequest => activeRequestCount > 0;

        public string ServerAddress => appSettings.ApiEndpoint;

        public async void Update()
        {
            if (!SessionStarted)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref updateCounter, 1, 0) == 1)
            {
                return;
            }

            //if (lastLogin > DateTime.MinValue && DateTime.UtcNow - lastLogin >= tokenRefreshInterval)
            //{
            //    await InvalidateAuthTokenAsync();
            //}

            if (!await Stream.UpdateAsync())
            {
                logger.Debug("Reconnecting to server...");
            }

            Interlocked.Decrement(ref updateCounter);
        }

        //private Task InvalidateAuthTokenAsync()
        //{
        //    return LoginAsync(username, password);
        //}

        public async Task<bool> SavePlayerAsync(PlayerController player)
        {
            if (!SessionStarted) return false;
            if (!Stream.IsReady) return false;
            return await Stream.SavePlayerAsync(player);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                var authToken = await Auth.AuthenticateAsync(username, password);
                if (authToken != null)
                {
                    lastLogin = DateTime.UtcNow;

                    // TODO(zerratar): fix me, this is bad. Dont want to have these values
                    //                 stored in memory

                    //this.username = username;
                    //this.password = password;

                    // bump
                    currentAuthToken = authToken;
                    tokenProvider.SetAuthToken(currentAuthToken);
                    gameManager.OnAuthenticated();
                    return true;
                }
            }
            catch (Exception exc)
            {
                logger.Error(exc.ToString());
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
                var sessionToken = await Game.BeginSessionAsync(clientVersion, accessKey, useLocalPlayers);
                if (sessionToken != null)
                {
                    tokenProvider.SetSessionToken(sessionToken);
                    currentSessionToken = sessionToken;
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
                logger.Error(exc.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
            return false;
        }

        public async Task<RavenNest.Models.Player> PlayerJoinAsync(string userId, string username)
        {
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                return await Players.PlayerJoinAsync(userId, username);
            }
            catch (Exception exc)
            {
                logger.Error(exc.ToString());
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
                logger.Error(exc.ToString());
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
                logger.Error(exc.ToString());
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
    }
}
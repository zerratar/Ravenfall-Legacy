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
using RavenNest.SDK.V2;
using Shinobytes.DeltaTcpLib;

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
            Settings = LocalDevServer.Enabled ? new LocalDevServerEndpoint() :
                        //new UnsecureLocalRavenNestStreamSettings()
                        new ProductionEndpoint()
                        //new StagingRavenNestStreamSettings()

                        //new LocalServerRemoteBotEndpoint()
                        //new DevServerRemoteBotEndpoint()
                        //new LocalEndpoint()
                        ;

            ServicePointManager.DefaultConnectionLimit = 2000;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateCertificate);

            this.logger = logger ?? new UnityLogger();
            this.gameManager = gameManager;
            var binarySerializer = new CompressedJsonSerializer();//new BinarySerializer();

            tokenProvider = new TokenProvider();
            var request = new WebApiRequestBuilderProvider(Settings, tokenProvider);

            Tcp = new TcpApi(gameManager, Settings.TcpApiEndpoint, Settings.TcpApiPort, tokenProvider);

            //Delta = new DeltaClient
            DeltaClient = new Shinobytes.DeltaTcpLib.DeltaClient(Settings.TcpApiEndpoint, Settings.TcpApiPort + 1, tokenProvider);
            //#if DEBUG
            //            DeltaClient = new Shinobytes.DeltaTcpLib.DeltaClient("127.0.0.1", Settings.TcpApiPort + 2, tokenProvider);
            //#else
            //            DeltaClient = new Shinobytes.DeltaTcpLib.DeltaClient(Settings.TcpApiEndpoint, Settings.TcpApiPort + 1, tokenProvider);
            //#endif

            Auth = new AuthApi(this, logger, request);
            Game = new GameApi(this, logger, request);
            Items = new ItemsApi(this, logger, request);
            Players = new PlayersApi(this, logger, request);
            Marketplace = new MarketplaceApi(this, logger, request);
            Village = new VillageApi(this, logger, request);
            Clan = new ClanApi(this, logger, request);
            Shop = new ShopApi(request);
            Quests = new QuestsApi(request);
            Boats = new BoatsApi(request);
            botPlayerGenerator = new BotPlayerGenerator(gameManager);

            thread = new System.Threading.Thread(UpdateProcess);
            thread.Start();
        }

        public TcpApi Tcp { get; }
        public DeltaClient DeltaClient { get; }

        /// <summary>
        /// Whether to try the v2 connection (docs/network-v2.md) when the server offers it. -network-v1 on the
        /// command line keeps the old sockets.
        /// </summary>
        public static bool PreferV2 = !Environment.GetCommandLineArgs().Any(x => x.Equals("-network-v1", StringComparison.OrdinalIgnoreCase));

        /// <summary>True while this session saves, gets events and runs commands over the v2 connection.</summary>
        public bool UseV2 { get; private set; }

        /// <summary>The v2 connection's main thread side, while <see cref="UseV2"/>.</summary>
        public NetworkV2 V2 { get; private set; }

        private GameConnectionV2 v2Connection;
        private string clientVersion;
        private DateTime lastV2TokenRefresh;

        /// <summary>
        /// Whether the server link that carries events and saves is up: the v2 connection, or the old event
        /// socket. What the game checks before starting raids, dungeons and the like.
        /// </summary>
        public bool ServerReady => UseV2 ? (V2?.IsReady ?? false) : Tcp.IsReady;

        /// <summary>Whether every server link is up (for the "connection lost" notice): v2, or both old sockets.</summary>
        public bool Connected => UseV2 ? (v2Connection?.IsReady ?? false) : (Tcp.IsReady && DeltaClient.IsConnected);

        public AuthApi Auth { get; }
        public GameApi Game { get; }
        public ItemsApi Items { get; }
        public PlayersApi Players { get; }
        public MarketplaceApi Marketplace { get; }

        public ClanApi Clan { get; }
        public ShopApi Shop { get; }
        public QuestsApi Quests { get; }
        public BoatsApi Boats { get; }
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

        internal void SendGameState()
        {
            if (Tcp != null) Tcp.SendGameState();
        }

        internal void SavePlayerExperience(IReadOnlyList<PlayerController> players, bool saveAllSkills = false)
        {
            if (Tcp != null) Tcp.SavePlayerExperience(players, saveAllSkills);
        }

        internal void SavePlayerState(IReadOnlyList<PlayerController> players)
        {
            if (Tcp != null) Tcp.SavePlayerState(players);
        }

        /// <summary>Why the last login failed, for the login screen.</summary>
        public LoginFailure LastLoginFailure { get; private set; }

        /// <summary>The website these endpoints belong to, "https://www.ravenfall.stream", for links to it.</summary>
        public string WebsiteUrl
        {
            get
            {
                var url = Settings.WebApiAuthEndpoint ?? string.Empty;
                var api = url.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
                // the local server is called by address for speed, but signed in to by name
                return (api > 0 ? url.Substring(0, api) : url).TrimEnd('/').Replace("://127.0.0.1", "://localhost");
            }
        }

        /// <summary>Signs in with a device token, from a Twitch sign in or remember me.</summary>
        public async Task<bool> LoginWithDeviceAsync(string deviceToken)
        {
            LastLoginFailure = LoginFailure.None;
            try
            {
                Interlocked.Increment(ref activeRequestCount);
                var result = await Auth.DeviceLoginAsync(deviceToken);
                if (result != null && result.AuthToken != null)
                {
                    return CompleteLogin(result.AuthToken);
                }

                LastLoginFailure = LoginFailure.WrongCredentials;
            }
            catch (Exception exc)
            {
                LastLoginFailure = LoginFailure.Unreachable;
                logger.WriteError(exc.Message);
            }
            finally
            {
                Interlocked.Decrement(ref activeRequestCount);
            }
            return false;
        }

        /// <summary>Takes a token the server handed out some other way than a password, such as an approved Twitch sign in.</summary>
        public bool CompleteLogin(AuthToken authToken)
        {
            if (authToken == null || authToken.UserId == Guid.Empty || authToken.Expired)
            {
                return false;
            }

            currentAuthToken = authToken;
            tokenProvider.SetAuthToken(currentAuthToken);
            gameManager.OnAuthenticated();
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            LastLoginFailure = LoginFailure.None;
            try
            {
                if (Ravenfall.isBatchMode)
                {
                    Shinobytes.Debug.Log("Attempting to authenticate with server.");
                }

                Interlocked.Increment(ref activeRequestCount);
                var authToken = await Auth.AuthenticateAsync(username, password);
                if (authToken != null)
                {
                    currentAuthToken = authToken;
                    tokenProvider.SetAuthToken(currentAuthToken);
                    gameManager.OnAuthenticated();
                    return true;
                }

                LastLoginFailure = LoginFailure.WrongCredentials;
            }
            catch (Exception exc)
            {
                // a refusal comes back as Forbidden; anything else means the request never got an answer
                LastLoginFailure = exc.Message.Contains("Forbidden") ? LoginFailure.WrongCredentials : LoginFailure.Unreachable;
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
                var result = await Game.BeginSessionAsync(clientVersion, accessKey, SessionStartTime, GameUpdater.UpdateWasSkipped);

                if (result == null || result.State != BeginSessionResultState.Success || result.SessionToken == null)
                {
                    Interlocked.CompareExchange(ref badClientVersion, 1, 0);
                    await Task.Delay(250);
                    return false;
                }

                // the old event socket connects as soon as there is a session token; v2 replaces it when offered
                if (PreferV2)
                {
                    Tcp.Enabled = false;
                }

                tokenProvider.SetSessionToken(result.SessionToken);
                currentSessionToken = result.SessionToken;
                this.clientVersion = clientVersion;
                TwitchUserName = currentSessionToken.TwitchUserName;
                TwitchDisplayName = currentSessionToken.TwitchDisplayName;
                TwitchUserId = currentSessionToken.TwitchUserId;


                UserSettings = result.UserSettings;

                if (PreferV2)
                {
                    await StartV2Async();
                }

                AwaitingSessionStart = false;
                gameManager.OnSessionStart();
                gameManager.HandleGameEvent(result.Village);
                gameManager.HandleGameEvent(result.Permissions);
                gameManager.HandleGameEvent(result.ExpMultiplier);

                Shinobytes.Debug.Log("Session Started (Client Version: " + clientVersion + ")");
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

        internal async void PlayerRemoveFailedAsync(PlayerController player, string reason)
        {
            bool requestSent = false;
            try
            {
                if (player.IsBot && player.PlatformId.StartsWith("#"))
                {
                    return;
                }
                requestSent = true;
                Interlocked.Increment(ref activeRequestCount);
                await Players.PlayerRemoveFailedAsync(player.Id, reason);
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
            }
            finally
            {
                if (requestSent)
                    Interlocked.Decrement(ref activeRequestCount);
            }
        }

        internal async void PlayerRemoveAsync(PlayerController player)
        {
            bool requestSent = false;
            try
            {
                if (player.IsBot && player.PlatformId.StartsWith("#"))
                {
                    return;
                }
                requestSent = true;
                Interlocked.Increment(ref activeRequestCount);
                await Players.PlayerRemoveAsync(player.Id);
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
            }
            finally
            {
                if (requestSent)
                    Interlocked.Decrement(ref activeRequestCount);
            }
        }

        public async Task<RavenNest.Models.PlayerJoinResult> PlayerJoinAsync(PlayerJoinData joinData, int retry = 0)
        {
            bool requestSent = false;
            try
            {
                // way to fake a bot here.
                // we dont need platformId if we have a character id

                if (joinData.UserId != null && joinData.PlatformId != null && joinData.PlatformId.StartsWith("#"))
                {
                    return botPlayerGenerator.Generate(joinData);
                }

                if (retry >= 5)
                {
                    logger.WriteError("Unable to add the player: " + joinData.UserName + ", tried " + (retry) + " times.");
                    return null;
                }
                requestSent = true;
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
                if (requestSent)
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

        public bool EndSession()
        {
            try
            {
                //Interlocked.Increment(ref activeRequestCount);
                Game.EndSession();
                return true;
            }
            catch (Exception exc)
            {
                logger.WriteError(exc.Message);
                return false;
            }
            finally
            {
                //Interlocked.Decrement(ref activeRequestCount);
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

        internal void Terminate()
        {
            EndSession();
            Dispose();
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
                V2?.WriteJournal();
                v2Connection?.Dispose();
            }
            catch { }
            disposed = true;
        }

        /// <summary>
        /// Switches this session to the v2 connection when the server offers it (it hands out a connection token);
        /// otherwise the old sockets carry on.
        /// </summary>
        private async Task StartV2Async()
        {
            string token = null;
            try
            {
                token = await Game.GetConnectionTokenAsync();
            }
            catch (Exception exc)
            {
                logger.WriteWarning("The server does not offer the v2 connection (" + exc.Message + "); using the old one.");
            }

            if (string.IsNullOrEmpty(token))
            {
                FallBackToV1();
                return;
            }

            if (v2Connection == null)
            {
                v2Connection = new GameConnectionV2(Settings.WebApiEndpoint, clientVersion);
                V2 = new NetworkV2(gameManager, this, v2Connection);
            }

            UseV2 = true;
            Tcp.Enabled = false;
            v2Connection.Start(token);
            Shinobytes.Debug.Log("Using the v2 server connection: " + v2Connection.Uri);
        }

        /// <summary>The server refused the v2 connection for good. Main thread.</summary>
        internal async void OnV2Rejected(RavenNest.Protocol.Rejected rejected)
        {
            switch (rejected.Reason)
            {
                case RavenNest.Protocol.RejectReason.InvalidToken:
                    // the session was begun again, or the token is from before a server key change: get a new one
                    if (DateTime.UtcNow - lastV2TokenRefresh > TimeSpan.FromSeconds(30) && SessionStarted)
                    {
                        lastV2TokenRefresh = DateTime.UtcNow;
                        await StartV2Async();
                    }
                    break;

                case RavenNest.Protocol.RejectReason.ClientTooOld:
                    Interlocked.CompareExchange(ref badClientVersion, 1, 0);
                    break;

                case RavenNest.Protocol.RejectReason.Replaced:
                    // another copy of the game took over this session; leave it to that one
                    break;

                default:
                    FallBackToV1();
                    break;
            }
        }

        private void FallBackToV1()
        {
            if (UseV2)
            {
                Shinobytes.Debug.LogWarning("Going back to the old server connection.");
            }

            UseV2 = false;
            V2?.Commands.FailAll();
            v2Connection?.Stop();
            Tcp.Enabled = true;
        }

        internal void StartSession(string version, string accessKey)
        {
            if (Ravenfall.isBatchMode)
            {
                Shinobytes.Debug.Log("Attempting to start new game session.");
            }

            AwaitingSessionStart = true;
            SessionStartTime = UnityEngine.Time.time;
            startSessionTask = StartSessionAsync(version, accessKey);
        }

        internal void UploadStateDataAsync(Guid requestId, byte[] content, Action<string> onCompletion = null)
        {
            // upload the game state to the server, we need to make sure we include the same request id as we got from server.
            Game.UploadStateDataAsync(requestId, content)
                .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.WriteError("Failed to upload game state: " + t.Exception?.Message);
                    onCompletion?.Invoke("Failed to upload game state to server.");
                }
                else if (t.IsCompletedSuccessfully)
                {
                    logger.WriteDebug($"Game state uploaded successfully.");
                    onCompletion?.Invoke("Game state was uploaded successfully.");
                }
            });
        }

        internal void UploadPlayerLogAsync(Guid requestId, byte[] content, Action<string> onCompletion = null)
        {
            // upload the log file to the server, we need to make sure we include the same request id as we got from server.

            Game.UploadLogFileAsync(requestId, content)
                .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.WriteError("Failed to upload log file: " + t.Exception?.Message);
                    onCompletion?.Invoke("Failed to upload log file to server.");
                }
                else if (t.IsCompletedSuccessfully)
                {
                    logger.WriteDebug($"Player log uploaded successfully.");
                    onCompletion?.Invoke("Log file was uploaded successfully.");
                }
            });

        }
    }

    public enum LoginFailure
    {
        None,
        /// <summary>The server answered and turned the name and password down.</summary>
        WrongCredentials,
        /// <summary>No answer from the server: offline, blocked, or the server is down.</summary>
        Unreachable,
    }
}

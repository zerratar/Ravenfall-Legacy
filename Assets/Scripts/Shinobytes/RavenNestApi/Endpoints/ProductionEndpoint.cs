namespace RavenNest.SDK.Endpoints
{
    public class ProductionEndpoint : IAppSettings
    {
        //#if UNITY_STANDALONE_LINUX
        //#endif
        public string WebApiEndpoint => "https://www.ravenfall.stream/api/";
        public string WebApiAuthEndpoint => "https://www.ravenfall.stream/api/";
        public string WebSocketEndpoint => "wss://www.ravenfall.stream/api/stream";
        public string TcpApiEndpoint => "ravenfall.stream";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";

        public int TcpApiPort => 3920;
        //public string ApiEndpoint => "https://www.ravenfall.stream/api/";
        //public string ApiAuthEndpoint => "https://www.ravenfall.stream/api/";
        //public string WebSocketEndpoint => "wss://www.ravenfall.stream/api/stream";
    }

    public class StagingRavenNestStreamSettings : IAppSettings
    {
        public string WebApiEndpoint => "https://staging.ravenfall.stream/api/";
        public string WebApiAuthEndpoint => "https://staging.ravenfall.stream/api/";
        public string WebSocketEndpoint => "wss://staging.ravenfall.stream/api/stream";
        public string TcpApiEndpoint => "staging.ravenfall.stream";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
        public int TcpApiPort => 3920;
    }
    public class UnsecureLocalRavenNestStreamSettings : IAppSettings
    {
        public string WebApiEndpoint => "http://localhost:5000/api/";
        public string WebApiAuthEndpoint => "http://localhost:5000/api/";
        public string WebSocketEndpoint => "ws://localhost:5000/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
        public int TcpApiPort => 3920;
    }
    public class LocalServerRemoteBotEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
        public int TcpApiPort => 1337;
    }
    public class DevServerRemoteBotEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://92.35.43.91:5001/api/";
        public string WebApiAuthEndpoint => "https://92.35.43.91:5001/api/";
        public string WebSocketEndpoint => "wss://92.35.43.91:5001/api/stream";
        public string TcpApiEndpoint => "92.35.43.91";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
        public int TcpApiPort => 3920;
    }
    public class LocalEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "127.0.0.1";
        public int TcpApiPort => 1337;
    }

    /// <summary>
    /// A RavenNest running on this machine the way the server's own settings start it: plain HTTP on
    /// port 80 and the game socket on 3920. For testing server changes before they are deployed.
    /// </summary>
    public class LocalDevServerEndpoint : IAppSettings
    {
        // 127.0.0.1 and https straight away. "localhost" tried IPv6 first, which the server does
        // not listen on, and Windows spends two seconds on a refused connection before it falls
        // back; the http address then redirected to https and paid it again. Every call to the
        // local server took four seconds. Links the browser opens still say localhost, see
        // RavenNestClient.WebsiteUrl, so the site's cookies and the Twitch return address match.
        public string WebApiEndpoint => "https://127.0.0.1/api/";
        public string WebApiAuthEndpoint => "https://127.0.0.1/api/";
        public string WebSocketEndpoint => "ws://localhost/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
        public int TcpApiPort => 3920;
    }

    /// <summary>
    /// Whether to use the local server: Ravenfall > Use Local RavenNest Server in the editor, or
    /// -localserver on the command line of a build. Production otherwise.
    /// </summary>
    public static class LocalDevServer
    {
        public const string PrefKey = "Ravenfall.UseLocalRavenNest";

        public static bool Enabled
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorPrefs.GetBool(PrefKey, false)) return true;
#endif
                foreach (var arg in System.Environment.GetCommandLineArgs())
                {
                    if (string.Equals(arg, "-localserver", System.StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }
        }
    }
}
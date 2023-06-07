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
    }
    public class UnsecureLocalRavenNestStreamSettings : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "ws://localhost:5000/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
    }
    public class LocalServerRemoteBotEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
    }
    public class DevServerRemoteBotEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://92.35.43.91:5001/api/";
        public string WebApiAuthEndpoint => "https://92.35.43.91:5001/api/";
        public string WebSocketEndpoint => "wss://92.35.43.91:5001/api/stream";
        public string TcpApiEndpoint => "92.35.43.91";
        public string RavenbotEndpoint => "ravenbot.ravenfall.stream";
    }
    public class LocalEndpoint : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
        public string RavenbotEndpoint => "127.0.0.1";
    }
}
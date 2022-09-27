namespace RavenNest.SDK.Endpoints
{
    public class ProductionRavenNestStreamSettings : IAppSettings
    {
        //#if UNITY_STANDALONE_LINUX
        //#endif
        public string WebApiEndpoint => "https://www.ravenfall.stream/api/";
        public string WebApiAuthEndpoint => "https://www.ravenfall.stream/api/";
        public string WebSocketEndpoint => "wss://www.ravenfall.stream/api/stream";
        public string TcpApiEndpoint => "ravenfall.stream";

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
    }
    public class UnsecureLocalRavenNestStreamSettings : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "ws://localhost:5000/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
    }
    public class LocalRavenNestStreamSettings : IAppSettings
    {
        public string WebApiEndpoint => "https://localhost:5001/api/";
        public string WebApiAuthEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
        public string TcpApiEndpoint => "127.0.0.1";
    }
}
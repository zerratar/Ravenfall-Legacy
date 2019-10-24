namespace RavenNest.SDK.Endpoints
{
    public class RavenNestStreamSettings : IAppSettings
    {
        public string ApiEndpoint => "https://www.ravenfall.stream/api/";
        public string WebSocketEndpoint => "wss://www.ravenfall.stream/api/stream";
    }

    public class LocalRavenNestStreamSettings : IAppSettings
    {
        public string ApiEndpoint => "https://localhost:5001/api/";
        public string WebSocketEndpoint => "wss://localhost:5001/api/stream";
    }
}
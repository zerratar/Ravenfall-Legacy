using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public class AuthApi
    {
        private readonly RavenNestClient client;
        private readonly ILogger logger;
        private readonly IApiRequestBuilderProvider request;

        public AuthApi(RavenNestClient client, ILogger logger, IApiRequestBuilderProvider request)
        {
            this.client = client;
            this.logger = logger;
            this.request = request;
        }

        /// <summary>
        /// A token, or null when the server turned the name and password down. Throws when the server
        /// could not be reached, so the login screen can tell the two apart.
        /// </summary>
        public Task<AuthToken> AuthenticateAsync(string username, string password)
        {
            return request.Create()
                .AddParameter("Username", username)
                .AddParameter("Password", password)
                .Build()
                .SendAsync<AuthToken>(ApiRequestTarget.Auth, ApiRequestType.Post, throwOnError: true);
        }

        // ---- Signing in through the website, and again with a device token ----
        // api/auth/device on the server; see ClientLoginManager there.

        public class DeviceStart
        {
            public string Code { get; set; }
            public string DisplayCode { get; set; }
            public string Path { get; set; }
            public int ExpiresInSeconds { get; set; }
        }

        public class DevicePoll
        {
            /// <summary>pending, approved, denied, expired or unknown.</summary>
            public string Status { get; set; }
            public AuthToken AuthToken { get; set; }
            public string DeviceToken { get; set; }
            public string UserName { get; set; }
        }

        public class DeviceLogin
        {
            public AuthToken AuthToken { get; set; }
            public string UserName { get; set; }
        }

        private class StartModel
        {
            public string Secret { get; set; }
            public string DeviceName { get; set; }
        }

        private class PollModel
        {
            public string Code { get; set; }
            public string Secret { get; set; }
        }

        private class DeviceModel
        {
            public string DeviceToken { get; set; }
            public string DeviceName { get; set; }
        }

        /// <summary>Starts a sign in through the website. Throws when the server cannot be reached.</summary>
        public Task<DeviceStart> StartDeviceLoginAsync(string secret, string deviceName)
        {
            return request.Create().Identifier("device").Method("start").Build()
                .SendAsync<DeviceStart, StartModel>(ApiRequestTarget.Auth, ApiRequestType.Post,
                    new StartModel { Secret = secret, DeviceName = deviceName }, throwOnError: true);
        }

        /// <summary>Whether the sign in was approved. Null when the server did not answer.</summary>
        public Task<DevicePoll> PollDeviceLoginAsync(string code, string secret)
        {
            return request.Create().Identifier("device").Method("poll").Build()
                .SendAsync<DevicePoll, PollModel>(ApiRequestTarget.Auth, ApiRequestType.Post,
                    new PollModel { Code = code, Secret = secret });
        }

        /// <summary>
        /// Signs in with a device token. Null when the server refused it; throws when the server
        /// could not be reached, so a token is only thrown away when it really is no good.
        /// </summary>
        public Task<DeviceLogin> DeviceLoginAsync(string deviceToken)
        {
            return request.Create().Identifier("device").Method("login").Build()
                .SendAsync<DeviceLogin, DeviceModel>(ApiRequestTarget.Auth, ApiRequestType.Post,
                    new DeviceModel { DeviceToken = deviceToken }, throwOnError: true);
        }

        /// <summary>A device token for this client, which must be signed in. Null on failure.</summary>
        public Task<string> RegisterDeviceAsync(string deviceName)
        {
            return request.Create().Identifier("device").Method("register").Build()
                .SendAsync<string, DeviceModel>(ApiRequestTarget.Auth, ApiRequestType.Post,
                    new DeviceModel { DeviceName = deviceName });
        }

        /// <summary>Forgets the device on the server, as logging out does.</summary>
        public Task RevokeDeviceAsync(string deviceToken)
        {
            return request.Create().Identifier("device").Method("revoke").Build()
                .SendAsync<object, DeviceModel>(ApiRequestTarget.Auth, ApiRequestType.Post,
                    new DeviceModel { DeviceToken = deviceToken });
        }
    }
}

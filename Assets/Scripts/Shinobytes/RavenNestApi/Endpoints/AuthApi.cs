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

        public Task<AuthToken> AuthenticateAsync(string username, string password)
        {
            return request.Create()
                .AddParameter("Username", username)
                .AddParameter("Password", password)
                .Build()
                .SendAsync<AuthToken>(ApiRequestTarget.Auth, ApiRequestType.Post);
        }
    }
}
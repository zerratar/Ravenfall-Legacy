using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    internal class WebBasedAuthEndpoint : IAuthEndpoint
    {
        private readonly IRavenNestClient client;
        private readonly ILogger logger;
        private readonly IApiRequestBuilderProvider request;

        public WebBasedAuthEndpoint(IRavenNestClient client, ILogger logger, IApiRequestBuilderProvider request)
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
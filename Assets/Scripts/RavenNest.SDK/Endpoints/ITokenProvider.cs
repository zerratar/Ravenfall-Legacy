using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface ITokenProvider
    {
        void SetAuthToken(AuthToken token);
        void SetSessionToken(SessionToken token);
        AuthToken GetAuthToken();
        SessionToken GetSessionToken();
    }

    public class TokenProvider : ITokenProvider
    {
        private AuthToken authToken;
        private SessionToken sessionToken;

        public AuthToken GetAuthToken() => authToken;

        public SessionToken GetSessionToken() => sessionToken;

        public void SetAuthToken(AuthToken token)
        {
            authToken = token;
        }

        public void SetSessionToken(SessionToken token)
        {
            sessionToken = token;
        }
    }

}
using System.Threading.Tasks;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public interface IAuthEndpoint
    {
        Task<AuthToken> AuthenticateAsync(string username, string password);
    }
}
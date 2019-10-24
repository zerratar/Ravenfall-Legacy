using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace RavenNest.SDK
{
    internal class NoCheckCertificatePolicy : System.Net.ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }
    }
}
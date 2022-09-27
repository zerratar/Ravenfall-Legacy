using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    internal class WebApiRequest : IApiRequest
    {
        private readonly IAppSettings settings;
        private readonly IRequestParameter[] parameters;
        private readonly string identifier;
        private readonly string method;
        private readonly CookieContainer cookieContainer;
        private readonly AuthToken authToken;
        private readonly SessionToken sessionToken;

        public WebApiRequest(
            CookieContainer cookieContainer,
            AuthToken authToken,
            SessionToken sessionToken,
            IAppSettings settings,
            string identifier,
            string method,
            params IRequestParameter[] parameters)
        {
            this.settings = settings;
            this.identifier = identifier;
            this.method = method;
            this.parameters = parameters;
            this.cookieContainer = cookieContainer;
            this.authToken = authToken;
            this.sessionToken = sessionToken;
        }

        public Task<TResult> SendAsync<TResult>(ApiRequestTarget reqTarget, ApiRequestType type)
        {
            return SendAsync<TResult, object>(reqTarget, type, null);
        }

        public Task SendAsync(ApiRequestTarget target, ApiRequestType type)
        {
            return SendAsync<object>(target, type);
        }

        public Task<TResult> SendAsync<TResult, TModel>(ApiRequestTarget reqTarget, ApiRequestType type, TModel model)
        {
            return SendAsync<TResult>(reqTarget, type, model);
        }

        public async Task<TResult> SendAsync<TResult>(ApiRequestTarget reqTarget, ApiRequestType type, object model)
        {
            if (IntegrityCheck.IsCompromised)
            {
                return default(TResult);
            }

            // string target, string method, 
            var target = GetTargetUrl(reqTarget);
            var request = (HttpWebRequest)WebRequest.CreateDefault(new Uri(target, UriKind.Absolute));
            var requestData = "";
            //request.Accept = "application/json";

            if (reqTarget == ApiRequestTarget.Game || reqTarget == ApiRequestTarget.Players)
            {
                request.Timeout = 25000;
            }

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
            request.Method = GetMethod(type);
            request.CookieContainer = cookieContainer;
            request.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            if (authToken != null)
            {
                request.Headers["auth-token"] = JsonConvert.SerializeObject(authToken).Base64Encode();
            }

            if (sessionToken != null)
            {
                request.Headers["session-token"] = JsonConvert.SerializeObject(sessionToken).Base64Encode();
            }

            if (parameters != null)
            {
                var named = parameters.Where(x => !string.IsNullOrEmpty(x.Key)).ToList();
                if (model != null)
                {
                    foreach (var param in named)
                    {
                        request.Headers[param.Key] = param.Value;
                    }
                }
                else if (named.Count > 0)
                {
                    requestData = "{" + string.Join(",", named.Select(x => "\"" + x.Key + "\": " + x.Value)) + "}";
                }
            }

            if (model != null)
            {
                requestData = JsonConvert.SerializeObject(model);
            }

            if (!string.IsNullOrEmpty(requestData))
            {
                request.ContentType = "application/json";
                request.ContentLength = Encoding.UTF8.GetByteCount(requestData);
                using (var reqStream = await request.GetRequestStreamAsync())
                using (var writer = new StreamWriter(reqStream))
                {
                    await writer.WriteAsync(requestData);
                    await writer.FlushAsync();
                }
            }

            try
            {
                using (var response = await request.GetResponseAsync())
                using (var resStream = response.GetResponseStream())
                using (var reader = new StreamReader(resStream))
                {
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.Forbidden)
                    {
                        return default(TResult);
                    }

                    if (typeof(TResult) == typeof(object))
                    {
                        return default(TResult);
                    }

                    var responseData = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<TResult>(responseData);
                }
            }
            catch (Exception exc)
            {
                try
                {
                    Shinobytes.Debug.LogError(exc);
                }
                catch { }
                return default(TResult);
            }
        }

        private string GetMethod(ApiRequestType type)
        {
            switch (type)
            {
                case ApiRequestType.Post: return HttpMethod.Post.Method;
                case ApiRequestType.Update: return HttpMethod.Put.Method;
                case ApiRequestType.Remove: return HttpMethod.Delete.Method;
                default: return HttpMethod.Get.Method;
            }
        }

        private string GetTargetUrl(ApiRequestTarget reqTarget)
        {
            var url = reqTarget == ApiRequestTarget.Auth ? settings.WebApiAuthEndpoint : settings.WebApiEndpoint;
            if (!url.EndsWith("/")) url += "/";
            url += reqTarget + "/";
            if (!string.IsNullOrEmpty(identifier)) url += $"{identifier}/";
            if (!string.IsNullOrEmpty(method)) url += $"{method}/";
            if (parameters == null) return url;
            var parameterString = string.Join("/", parameters.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => x.Value));
            if (!string.IsNullOrEmpty(parameterString)) url += $"{parameterString}";
            return url;
        }
    }

    public static class StringExtensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
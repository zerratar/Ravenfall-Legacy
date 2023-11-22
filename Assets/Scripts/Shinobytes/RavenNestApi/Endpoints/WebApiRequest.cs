using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenNest.Models;
using static GameMath;

namespace RavenNest.SDK.Endpoints
{
    public class WebApiRequest
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

        public Task<TResult> SendAsync<TResult>(ApiRequestTarget reqTarget, ApiRequestType type, bool throwOnError = false)
        {
            return SendAsync<TResult, object>(reqTarget, type, null, throwOnError);
        }

        public Task SendAsync(ApiRequestTarget target, ApiRequestType type, bool throwOnError = false)
        {
            return SendAsync<object>(target, type, throwOnError);
        }

        public Task<TResult> SendAsync<TResult, TModel>(ApiRequestTarget reqTarget, ApiRequestType type, TModel model, bool throwOnError = false)
        {
            return SendAsync<TResult>(reqTarget, type, model, throwOnError);
        }

        public async Task<TResult> SendAsync<TResult>(ApiRequestTarget reqTarget, ApiRequestType type, object model, bool throwOnError = false)
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

            request.ServicePoint.ConnectionLimit = 100;
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

            string responseData = "";

            try
            {
                using (var response = await request.GetResponseAsync())
                using (var resStream = response.GetResponseStream())
                using (var reader = new StreamReader(resStream))
                {
                    var r = ((HttpWebResponse)response);

                    responseData = await reader.ReadToEndAsync();

                    if (r.StatusCode == HttpStatusCode.Forbidden)
                    {
                        if (throwOnError) throw new Exception("Request returned status code Forbidden");
                        return default(TResult);
                    }
                    else if (r.StatusCode != HttpStatusCode.OK)
                    {
#if UNITY_EDITOR
                        Shinobytes.Debug.LogError(target + " request returned non OK status code: " + r.StatusCode + ", data: " + responseData);
#endif
                    }
                    if (typeof(TResult) == typeof(object))
                    {
                        return default(TResult);
                    }

                    return JsonConvert.DeserializeObject<TResult>(responseData);
                }
            }
            catch (Exception exc)
            {
                if (throwOnError) throw;
                try
                {
                    Shinobytes.Debug.LogError("WebApiRequest.SendAsync: " + type.ToString().ToUpper() + " " + GetTargetUrl(reqTarget, false) + " - " + exc.Message); //+ " - " + responseData);
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

        private string GetTargetUrl(ApiRequestTarget reqTarget, bool includeParameterValues = true)
        {
            var url = reqTarget == ApiRequestTarget.Auth ? settings.WebApiAuthEndpoint : settings.WebApiEndpoint;
            if (!url.EndsWith("/")) url += "/";
            url += reqTarget + "/";
            if (!string.IsNullOrEmpty(identifier)) url += $"{identifier}/";
            if (!string.IsNullOrEmpty(method)) url += $"{method}/";
            if (parameters == null) return url;

#if DEBUG
            var parameterString = string.Join("/", parameters.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => x.Value));
            if (!string.IsNullOrEmpty(parameterString)) url += $"{parameterString}";
            return url;
#else
            if (includeParameterValues)
            {
                var parameterString = string.Join("/", parameters.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => x.Value));
                if (!string.IsNullOrEmpty(parameterString)) url += $"{parameterString}";
            }
            else
            {
                var parameterString = string.Join("/", parameters.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => "hidden-value"));
                if (!string.IsNullOrEmpty(parameterString)) url += $"{parameterString}";
            }
            return url;
#endif


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
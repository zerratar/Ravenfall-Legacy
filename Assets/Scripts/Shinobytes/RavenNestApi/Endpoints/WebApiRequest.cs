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
        public void Send(ApiRequestTarget target, ApiRequestType type, bool throwOnError = false)
        {
            Send<object>(target, type, throwOnError);
        }
        public TResult Send<TResult>(ApiRequestTarget reqTarget, ApiRequestType type, bool throwOnError = false)
        {
            return Send<TResult, object>(reqTarget, type, null, throwOnError);
        }
        public TResult Send<TResult, TModel>(ApiRequestTarget reqTarget, ApiRequestType type, TModel model, bool throwOnError = false)
        {
            return Send<TResult>(reqTarget, type, model, throwOnError);
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

            var target = GetTargetUrl(reqTarget);
            var httpMethod = new HttpMethod(GetMethod(type));

            // Create a handler that accepts any certificates and uses our cookie container.
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using (var client = new HttpClient(handler))
            {
                // Set timeout for specific targets.
                if (reqTarget == ApiRequestTarget.Game || reqTarget == ApiRequestTarget.Players)
                {
                    client.Timeout = TimeSpan.FromMilliseconds(25000);
                }

                // Build the request message.
                var request = new HttpRequestMessage(httpMethod, target);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");

                // Add token headers if available.
                if (authToken != null)
                {
                    request.Headers.Add("auth-token", JsonConvert.SerializeObject(authToken).Base64Encode());
                }
                if (sessionToken != null)
                {
                    request.Headers.Add("session-token", JsonConvert.SerializeObject(sessionToken).Base64Encode());
                }

                // If parameters are provided, either add them as headers (if model is provided) or use them as request body.
                if (parameters != null)
                {
                    var named = parameters.Where(x => !string.IsNullOrEmpty(x.Key)).ToList();
                    if (model != null)
                    {
                        foreach (var param in named)
                        {
                            request.Headers.Add(param.Key, param.Value);
                        }
                    }
                    else if (named.Count > 0)
                    {
                        var parameterJson = "{" + string.Join(",", named.Select(x => $"\"{x.Key}\": {x.Value}")) + "}";
                        request.Content = new StringContent(parameterJson, Encoding.UTF8, "application/json");
                    }
                }

                // If a model is provided, serialize it as JSON and add it as the request body.
                if (model != null)
                {
                    if (model is byte[] rawBytes)
                    {
                        var stream = new MemoryStream(rawBytes);
                        request.Content = new StreamContent(stream);
                        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    }
                    else if (model is Stream streamModel)
                    {
                        request.Content = new StreamContent(streamModel);
                        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    }
                    else
                    {
                        var requestData = JsonConvert.SerializeObject(model);
                        request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");
                    }

                }

                try
                {
                    using (var response = await client.SendAsync(request))
                    {
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            if (throwOnError)
                                throw new Exception("Request returned status code Forbidden");
                            return default(TResult);
                        }
                        else if (response.StatusCode != HttpStatusCode.OK)
                        {
#if UNITY_EDITOR
                            Shinobytes.Debug.LogError($"{target} request returned non OK status code: {response.StatusCode}, data: {responseData}");
#endif
                        }

                        if (typeof(TResult) == typeof(object))
                            return default(TResult);

                        return JsonConvert.DeserializeObject<TResult>(responseData);
                    }
                }
                catch (Exception exc)
                {
                    if (throwOnError)
                        throw;
                    try
                    {
                        Shinobytes.Debug.LogError("WebApiRequest.SendAsync: " + type.ToString().ToUpper() + " " + GetTargetUrl(reqTarget) + " - " + exc.Message);
                    }
                    catch { }
                    return default(TResult);
                }
            }
        }

        public TResult Send<TResult>(ApiRequestTarget reqTarget, ApiRequestType type, object model, bool throwOnError = false)
        {
            if (IntegrityCheck.IsCompromised)
            {
                return default(TResult);
            }

            var target = GetTargetUrl(reqTarget);
            var httpMethod = new HttpMethod(GetMethod(type));

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using (var client = new HttpClient(handler))
            {
                if (reqTarget == ApiRequestTarget.Game || reqTarget == ApiRequestTarget.Players)
                {
                    client.Timeout = TimeSpan.FromMilliseconds(25000);
                }

                var request = new HttpRequestMessage(httpMethod, target);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");

                if (authToken != null)
                {
                    request.Headers.Add("auth-token", JsonConvert.SerializeObject(authToken).Base64Encode());
                }
                if (sessionToken != null)
                {
                    request.Headers.Add("session-token", JsonConvert.SerializeObject(sessionToken).Base64Encode());
                }
                if (parameters != null)
                {
                    var named = parameters.Where(x => !string.IsNullOrEmpty(x.Key)).ToList();
                    if (model != null)
                    {
                        foreach (var param in named)
                        {
                            request.Headers.Add(param.Key, param.Value);
                        }
                    }
                    else if (named.Count > 0)
                    {
                        var parameterJson = "{" + string.Join(",", named.Select(x => $"\"{x.Key}\": {x.Value}")) + "}";
                        request.Content = new StringContent(parameterJson, Encoding.UTF8, "application/json");
                    }
                }

                if (model != null)
                {
                    var requestData = JsonConvert.SerializeObject(model);
                    request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");
                }

                try
                {
                    // Block on the async call for a synchronous behavior.
                    var response = client.SendAsync(request).GetAwaiter().GetResult();
                    var responseData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        if (throwOnError)
                            throw new Exception("Request returned status code Forbidden");
                        return default(TResult);
                    }
                    else if (response.StatusCode != HttpStatusCode.OK)
                    {
#if UNITY_EDITOR
                        Shinobytes.Debug.LogError($"{target} request returned non OK status code: {response.StatusCode}, data: {responseData}");
#endif
                    }

                    if (typeof(TResult) == typeof(object))
                        return default(TResult);

                    return JsonConvert.DeserializeObject<TResult>(responseData);
                }
                catch (Exception exc)
                {
                    if (throwOnError)
                        throw;
                    try
                    {
                        Shinobytes.Debug.LogError("WebApiRequest.Send: " + type.ToString().ToUpper() + " " + GetTargetUrl(reqTarget) + " - " + exc.Message);
                    }
                    catch { }
                    return default(TResult);
                }
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
            if (parameters == null)
                return url;

            var parameterString = string.Join("/", parameters.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => x.Value));
            if (!string.IsNullOrEmpty(parameterString))
                url += $"{parameterString}";
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
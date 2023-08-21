using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using RavenNest.Models;

namespace RavenNest.SDK.Endpoints
{
    public class WebApiRequestBuilder : IApiRequestBuilder
    {
        private readonly CookieContainer sharedCookieContainer = new CookieContainer();
        private readonly List<IRequestParameter> parameters = new List<IRequestParameter>();
        private readonly IAppSettings appSettings;

        private readonly SessionToken sessionToken;
        private readonly AuthToken authToken;

        private string identifier;
        private string method;

        public WebApiRequestBuilder(IAppSettings appSettings, AuthToken authToken, SessionToken sessionToken)
        {
            this.appSettings = appSettings;
            this.authToken = authToken;
            this.sessionToken = sessionToken;
        }

        public IApiRequestBuilder Identifier(string value)
        {
            identifier = value;
            return this;
        }

        public IApiRequestBuilder AddParameter(string value)
        {
            parameters.Add(new WebApiRequestParameter(null, value));
            return this;
        }

        public IApiRequestBuilder AddParameter(string key, object value)
        {
            parameters.Add(new WebApiRequestParameter(key, JsonConvert.SerializeObject(value)));
            return this;
        }

        public IApiRequestBuilder Method(string item)
        {
            method = item;
            return this;
        }

        public WebApiRequest Build()
        {
            return new WebApiRequest(
                sharedCookieContainer,
                authToken,
                sessionToken,
                appSettings,
                identifier,
                method,
                parameters.ToArray());
        }
    }
}
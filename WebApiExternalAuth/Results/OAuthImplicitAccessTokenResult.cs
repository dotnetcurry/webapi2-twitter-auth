using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApiExternalAuth.Results
{
    public sealed class OAuthImplicitAccessTokenResponse
    {
        public string AccessToken { get; set; }

        public string TokenType { get; set; }

        public int? ExpiresIn { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }
    }

    public class OAuthImplicitAccessTokenResult : IHttpActionResult
    {
        public OAuthImplicitAccessTokenResult(string redirectUrl, OAuthImplicitAccessTokenResponse response,
            ApiController controller)
        {
            RedirectUrl = redirectUrl;
            Response = response;
            Request = controller.Request;
        }

        public string RedirectUrl { get; private set; }
        public OAuthImplicitAccessTokenResponse Response { get; private set; }
        public HttpRequestMessage Request { get; private set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Redirect);
            UriBuilder uriBuilder = new UriBuilder(new Uri(Request.RequestUri, RedirectUrl));
            FormUrlEncodedStringBuilder fragmentBuilder = new FormUrlEncodedStringBuilder();
            fragmentBuilder.Add("access_token", Response.AccessToken);
            fragmentBuilder.Add("token_type", Response.TokenType);

            if (Response.ExpiresIn.HasValue)
            {
                fragmentBuilder.Add("expires_in", Response.ExpiresIn.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!String.IsNullOrEmpty(Response.Scope))
            {
                fragmentBuilder.Add("scope", Response.Scope);
            }

            if (!String.IsNullOrEmpty(Response.State))
            {
                fragmentBuilder.Add("state", Response.State);
            }

            uriBuilder.Fragment = fragmentBuilder.Build();
            response.Headers.Location = uriBuilder.Uri;
            response.RequestMessage = Request;
            return Task.FromResult(response);
        }
    }
}

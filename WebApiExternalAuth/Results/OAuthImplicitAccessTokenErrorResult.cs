using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApiExternalAuth.Results
{
    public enum OAuthImplicitAccessTokenError
    {
        InvalidRequest,
        UnauthorizedClient,
        AccessDenied,
        UnsupportedResponseType,
        InvalidScope,
        ServerError,
        TemporarilyUnavailable
    }

    public sealed class OAuthImplicitAccessTokenErrorResponse
    {
        public OAuthImplicitAccessTokenError Error { get; set; }

        public string ErrorDescription { get; set; }

        public string ErrorUri { get; set; }

        public string State { get; set; }
    }

    public class OAuthImplicitAccessTokenErrorResult : IHttpActionResult
    {
        public OAuthImplicitAccessTokenErrorResult(string redirectUrl, OAuthImplicitAccessTokenError error,
            string state, ApiController controller)
            : this(redirectUrl, new OAuthImplicitAccessTokenErrorResponse { Error = error, State = state }, controller)
        {
        }

        public OAuthImplicitAccessTokenErrorResult(string redirectUrl, OAuthImplicitAccessTokenErrorResponse response,
            ApiController controller)
        {
            RedirectUrl = redirectUrl;
            Response = response;
            Request = controller.Request;
        }

        public string RedirectUrl { get; private set; }
        public OAuthImplicitAccessTokenErrorResponse Response { get; private set; }
        public HttpRequestMessage Request { get; private set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Redirect);
            UriBuilder uriBuilder = new UriBuilder(new Uri(Request.RequestUri, RedirectUrl));
            FormUrlEncodedStringBuilder fragmentBuilder = new FormUrlEncodedStringBuilder();
            fragmentBuilder.Add("error", GetErrorText(Response.Error));

            if (!String.IsNullOrEmpty(Response.ErrorDescription))
            {
                fragmentBuilder.Add("error_description", Response.ErrorDescription);
            }

            if (!String.IsNullOrEmpty(Response.ErrorUri))
            {
                fragmentBuilder.Add("error_uri", Response.ErrorUri);
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

        private static string GetErrorText(OAuthImplicitAccessTokenError error)
        {
            switch (error)
            {
                case OAuthImplicitAccessTokenError.InvalidRequest:
                    return "invalid_request";
                case OAuthImplicitAccessTokenError.UnauthorizedClient:
                    return "unauthorized_client";
                case OAuthImplicitAccessTokenError.AccessDenied:
                    return "access_denied";
                case OAuthImplicitAccessTokenError.UnsupportedResponseType:
                    return "unsupported_response_type";
                case OAuthImplicitAccessTokenError.InvalidScope:
                    return "invalid_scope";
                case OAuthImplicitAccessTokenError.ServerError:
                    return "server_error";
                case OAuthImplicitAccessTokenError.TemporarilyUnavailable:
                    return "temporarily_unavailable";
                default:
                    throw new ArgumentException("error");
            }
        }
    }
}

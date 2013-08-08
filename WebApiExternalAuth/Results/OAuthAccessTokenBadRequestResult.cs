using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebApiExternalAuth.Results
{
    public enum OAuthAccessTokenError
    {
        [EnumMember(Value = "invalid_request")]
        InvalidRequest,

        [EnumMember(Value = "invalid_client")]
        InvalidClient,

        [EnumMember(Value = "invalid_grant")]
        InvalidGrant,

        [EnumMember(Value = "unauthorized_client")]
        UnauthorizedClient,

        [EnumMember(Value = "unsupported_grant_type")]
        UnsupportedGrantType,

        [EnumMember(Value = "invalid_scope")]
        InvalidScope
    }

    public class OAuthAccessTokenErrorContent
    {
        [JsonProperty("error")]
        public OAuthAccessTokenError Error { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }

        [JsonProperty("error_uri")]
        public string ErrorUri { get; set; }
    }

    public class OAuthAccessTokenBadRequestResult : IHttpActionResult
    {
        public OAuthAccessTokenBadRequestResult(OAuthAccessTokenError error, string errorDescription,
            ApiController controller)
            : this(new OAuthAccessTokenErrorContent { Error = error, ErrorDescription = errorDescription }, controller)
        {
        }

        public OAuthAccessTokenBadRequestResult(OAuthAccessTokenErrorContent content, ApiController controller)
        {
            Content = content;
            Request = controller.Request;
        }

        public OAuthAccessTokenErrorContent Content { get; private set; }
        public HttpRequestMessage Request { get; private set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            string contentText;

            using (TextWriter writer = new StringWriter())
            {
                JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                serializer.Serialize(writer, Content);
                contentText = writer.ToString();
            }

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(contentText);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.RequestMessage = Request;
            return Task.FromResult(response);
        }
    }
}

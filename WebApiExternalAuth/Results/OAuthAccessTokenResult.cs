using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebApiExternalAuth.Results
{
    public class OAuthAccessTokenContent
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }
    }

    public class OAuthAccessTokenResult : IHttpActionResult
    {
        public OAuthAccessTokenResult(OAuthAccessTokenContent content, ApiController controller)
        {
            Content = content;
            Request = controller.Request;
        }

        public OAuthAccessTokenContent Content { get; private set; }
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

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(contentText);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.RequestMessage = Request;
            return Task.FromResult(response);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security;

namespace WebApiExternalAuth.Results
{
    public class ChallengeResult : IHttpActionResult
    {
        public ChallengeResult(string loginProvider, string returnUrl, ApiController controller)
        {
            LoginProvider = loginProvider;
            ReturnUrl = returnUrl;
            Request = controller.Request;
        }

        public string LoginProvider { get; set; }
        public string ReturnUrl { get; set; }
        public HttpRequestMessage Request { get; set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            Request.GetOwinRequest().Authentication.Challenge(new AuthenticationExtra { RedirectUrl = ReturnUrl },
                LoginProvider);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.RequestMessage = Request;
            return Task.FromResult(response);
        }
    }
}

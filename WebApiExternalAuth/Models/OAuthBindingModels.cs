using System;
using System.Web.Http;

namespace WebApiExternalAuth.Models
{
    // Models used as parameters to OAuth actions.

    [FromUri]
    public class OAuthImplicitAuthorizationRequestBindingModel
    {
        public string response_type { get; set; }

        public string client_id { get; set; }

        public string redirect_uri { get; set; }

        public string scope { get; set; }

        public string state { get; set; }
    }

    public class OAuthPasswordCredentialsBindingModel
    {
        public string grant_type { get; set; }

        public string username { get; set; }

        public string password { get; set; }

        public string scope { get; set; }
    }
}

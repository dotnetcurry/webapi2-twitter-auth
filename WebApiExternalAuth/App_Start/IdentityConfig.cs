using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Helpers;
using Microsoft.Owin.Security.OAuth;

namespace WebApiExternalAuth
{
    // For more information on ASP.NET Identity, visit http://go.microsoft.com/fwlink/?LinkId=301863
    public static class IdentityConfig
    {
        public static OAuthBearerAuthenticationOptions Bearer { get; set; }

        public static void ConfigureIdentity()
        {
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
            Bearer = new OAuthBearerAuthenticationOptions();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin.Security;
using Owin;

namespace WebApiExternalAuth
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerAuthentication(IdentityConfig.Bearer);

            // Enable the application to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie();

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            app.UseTwitterAuthentication(
                consumerKey: "FcO5sPaxkwarRu6Qtbvuw",
                consumerSecret: "PF5aivnyFZggSRqXFlnKPH65QT1st4Y2nqW7dRzlg");

            //app.UseFacebookAuthentication(
            //    appId: "",
            //    appSecret: "");

            //app.UseGoogleAuthentication();
        }
    }
}

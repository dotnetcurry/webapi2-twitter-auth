using System;
using System.Collections.Generic;
using WebApiExternalAuth.Results;

namespace WebApiExternalAuth.Models
{
    // Models returned by AccountController actions.

    public class ExternalLoginViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string State { get; set; }
    }

    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; }

        public string UserName { get; set; }

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
    }

    public class OAuthTokenViewModel : OAuthAccessTokenContent
    {
        public string UserName { get; set; }
    }

    public class RegisterExternalLoginViewModel
    {
        public string UserName { get; set; }

        public string LoginProvider { get; set; }
    }

    public class UserInfoViewModel
    {
        public string UserName { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }
}

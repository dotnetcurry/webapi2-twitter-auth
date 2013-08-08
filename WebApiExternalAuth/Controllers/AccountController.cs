using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.WebPages;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Forms;
using Microsoft.Owin.Security.OAuth;
using WebApiExternalAuth.Models;
using WebApiExternalAuth.Results;

namespace WebApiExternalAuth.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        public AccountController()
        {
            IdentityStore = new IdentityStoreManager(new IdentityStoreContext());
            AuthenticationManager = new IdentityAuthenticationManager(IdentityStore);
            Bearer = IdentityConfig.Bearer;
            ExternalTokenHandler = new ExternalAccessTokenHandler(IdentityConfig.Bearer.AccessTokenHandler);
        }

        public AccountController(IdentityStoreManager identityStore,
            IdentityAuthenticationManager authenticationManager, OAuthBearerAuthenticationOptions bearer,
            ISecureDataHandler<ExternalAccessToken> externalTokenHandler)
        {
            IdentityStore = identityStore;
            AuthenticationManager = authenticationManager;
            Bearer = bearer;
            ExternalTokenHandler = externalTokenHandler;
        }

        public IdentityStoreManager IdentityStore { get; private set; }
        public IdentityAuthenticationManager AuthenticationManager { get; private set; }
        public OAuthBearerAuthenticationOptions Bearer { get; private set; }
        public ISecureDataHandler<ExternalAccessToken> ExternalTokenHandler { get; private set; }

        // GET api/Account/UserInfo
        [HttpGet("UserInfo")]
        public UserInfoViewModel UserInfo()
        {
            return new UserInfoViewModel
            {
                UserName = User.Identity.GetUserName()
            };
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [HttpGet("ManageInfo")]
        public async Task<ManageInfoViewModel> ManageInfo(string returnUrl, bool generateState = false)
        {
            IList<IUserLogin> linkedAccounts = await IdentityStore.GetLogins(User.Identity.GetUserId());
            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IUserLogin linkedAccount in linkedAccounts)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = IdentityStore.LocalLoginProvider,
                UserName = User.Identity.GetUserName(),
                Logins = logins,
                ExternalLoginProviders = ExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [HttpPost("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await IdentityStore.ValidateLocalLogin(User.Identity.GetUserName(), model.OldPassword))
            {
                return BadRequest("The current password is incorrect.");
            }

            if (!await IdentityStore.Context.Secrets.Update(User.Identity.GetUserName(), model.NewPassword))
            {
                return BadRequest("The new password is invalid.");
            }

            await IdentityStore.Context.SaveChanges();

            return OK();
        }

        // POST api/Account/SetPassword
        [HttpPost("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Create the local login info and link the local account to the user
                if (!await IdentityStore.CreateLocalLogin(User.Identity.GetUserId(), User.Identity.GetUserName(),
                    model.NewPassword))
                {
                    return BadRequest("Failed to set password.");
                }
            }
            catch (IdentityException e)
            {
                return BadRequest(e.ToString());
            }

            return OK();
        }

        // POST api/Account/AddExternalLogin
        [HttpPost("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ExternalAccessToken token = ExternalTokenHandler.Unprotect(model.ExternalAccessToken);

            if (token == null || !token.IsValid)
            {
                return BadRequest("External login failure.");
            }

            string userId = await IdentityStore.GetUserIdForLogin(token.LoginProvider, token.ProviderKey);

            if (!String.IsNullOrEmpty(userId))
            {
                return BadRequest("The external login is already associated with an account.");
            }

            // The current user is logged in, just add the new account
            if (!await IdentityStore.AddLogin(User.Identity.GetUserId(), token.LoginProvider, token.ProviderKey))
            {
                return BadRequest("Failed to add the external login.");
            }

            return OK();
        }

        // POST api/Account/RemoveLogin
        [HttpPost("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await IdentityStore.Context.Logins.GetLogins(User.Identity.GetUserId())).Count <= 1)
            {
                return BadRequest("At least one login must remain for the account.");
            }

            if (!await IdentityStore.RemoveLogin(User.Identity.GetUserId(), model.LoginProvider, model.ProviderKey))
            {
                return BadRequest("Failed to remove the login.");
            }

            return OK();
        }

        // POST api/Account/Login
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IHttpActionResult> Login(OAuthPasswordCredentialsBindingModel model)
        {
            if (model == null)
            {
                return OAuthBadRequest(OAuthAccessTokenError.InvalidRequest);
            }

            if (model.grant_type != "password")
            {
                return OAuthBadRequest(OAuthAccessTokenError.UnsupportedGrantType);
            }

            if (!await IdentityStore.ValidateLocalLogin(model.username, model.password))
            {
                return OAuthBadRequest(OAuthAccessTokenError.InvalidRequest,
                    "The user name or password provided is incorrect.");
            }

            string userId = await IdentityStore.GetUserIdForLocalLogin(model.username);
            ClaimsIdentity identity = await GetIdentityAsync(userId);
            string token = CreateAccessToken(identity);
            IUser user = await IdentityStore.Context.Users.Find(userId);

            return OAuthAccessToken(token, "bearer", user.UserName);
        }

        // GET api/Account/ExternalLogin
        [AllowAnonymous]
        [HttpGet("ExternalLogin", RouteName = "ExternalLogin")]
        public IHttpActionResult ExternalLogin(string provider, OAuthImplicitAuthorizationRequestBindingModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            if (!String.IsNullOrEmpty(model.redirect_uri) && !ValidateRedirectUri(model.client_id, model.redirect_uri))
            {
                return BadRequest();
            }

            if (model.response_type != "token")
            {
                return OAuthImplicitError(model.redirect_uri, model.state,
                    OAuthImplicitAccessTokenError.UnsupportedResponseType);
            }

            if (model.client_id != "self")
            {
                return OAuthImplicitError(model.redirect_uri, model.state,
                    OAuthImplicitAccessTokenError.InvalidRequest);
            }

            string externalRedirectUrl = Url.Route("ExternalLoginCallback", new
            {
                clientId = model.client_id,
                redirectUrl = model.redirect_uri,
                state = model.state
            });

            return new ChallengeResult(provider, externalRedirectUrl, this);
        }

        // GET api/Account/ExternalLoginCallback
        [AllowAnonymous]
        [HttpGet("ExternalLoginCallback", RouteName = "ExternalLoginCallback")]
        public async Task<IHttpActionResult> ExternalLoginCallback(string clientId, string redirectUrl,
            string state = null)
        {
            if (String.IsNullOrEmpty(redirectUrl) || !ValidateRedirectUri(clientId, redirectUrl))
            {
                return BadRequest();
            }

            ClaimsIdentity identity = await Authentication.GetExternalIdentity();

            if (identity == null)
            {
                return OAuthImplicitError(redirectUrl, state, OAuthImplicitAccessTokenError.AccessDenied);
            }

            Authentication.SignOut(FormsAuthenticationDefaults.ExternalAuthenticationType);

            TimeSpan expiresIn = TimeSpan.FromHours(1);
            string token = CreateExternalAccessToken(identity, expiresIn);

            if (token == null)
            {
                return OAuthImplicitError(redirectUrl, state, OAuthImplicitAccessTokenError.ServerError);
            }

            OAuthImplicitAccessTokenResponse response = new OAuthImplicitAccessTokenResponse
            {
                AccessToken = token,
                TokenType = "bearer",
                ExpiresIn = (int)expiresIn.TotalSeconds,
                State = state
            };
            return new OAuthImplicitAccessTokenResult(redirectUrl, response, this);
        }

        // GET api/Account/ExternalLoginComplete
        [AllowAnonymous]
        [HttpGet("ExternalLoginComplete", RouteName = "ExternalLoginComplete")]
        public async Task<IHttpActionResult> ExternalLoginComplete(string access_token)
        {
            ExternalAccessToken externalToken = ExternalTokenHandler.Unprotect(access_token);

            if (externalToken == null || !externalToken.IsValid)
            {
                return BadRequest("External login failure.");
            }

            string userId = await IdentityStore.GetUserIdForLogin(externalToken.LoginProvider,
                externalToken.ProviderKey);

            if (String.IsNullOrEmpty(userId))
            {
                return Content(HttpStatusCode.OK, new RegisterExternalLoginViewModel
                {
                    UserName = externalToken.DisplayName,
                    LoginProvider = externalToken.LoginProvider
                });
            }

            ClaimsIdentity identity = await GetIdentityAsync(userId);
            string token = CreateAccessToken(identity);
            IUser user = await IdentityStore.Context.Users.Find(userId);

            return OAuthAccessToken(token, "bearer", user.UserName);
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [HttpGet("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> ExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                state = GenerateAntiForgeryState();
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = "self",
                        redirect_uri = returnUrl,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await IdentityStore.GetUserIdForLocalLogin(model.UserName) != null)
            {
                return BadRequest("A user with the name '{0}' already exists.", model.UserName);
            }

            // Create a profile, password, and link the local login before signing in the user
            User user = new User(model.UserName);

            try
            {
                if (!await IdentityStore.CreateLocalUser(user, model.Password))
                {
                    return BadRequest("Failed to create login for '{0}'.", model.UserName);
                }
            }
            catch (IdentityException e)
            {
                return BadRequest(e.Message);
            }

            InitiateDatabaseForNewUser(user.Id);
            ClaimsIdentity identity = await GetIdentityAsync(user.Id);
            string token = CreateAccessToken(identity);

            return OAuthAccessToken(token, "bearer", user.UserName);
        }

        // POST api/Account/RegisterExternal
        [AllowAnonymous]
        [HttpPost("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ExternalAccessToken externalToken = ExternalTokenHandler.Unprotect(model.ExternalAccessToken);

            if (externalToken == null || !externalToken.IsValid)
            {
                return BadRequest("External login failure.");
            }

            string existingUserId = await IdentityStore.GetUserIdForLogin(externalToken.LoginProvider,
                externalToken.ProviderKey);

            if (!String.IsNullOrEmpty(existingUserId))
            {
                return BadRequest("The external login is already associated with an account.");
            }

            if (await IdentityStore.GetUserIdForLocalLogin(model.UserName) != null)
            {
                return BadRequest("A user with the name '{0}' already exists.", model.UserName);
            }

            // Create a profile and link the local account before signing in the user
            User user = new User(model.UserName);

            try
            {
                if (!await IdentityStore.CreateExternalUser(user, externalToken.LoginProvider,
                    externalToken.ProviderKey))
                {
                    return BadRequest("Failed to create login for '{0}'.", model.UserName);
                }
            }
            catch (IdentityException e)
            {
                return BadRequest(e.ToString());
            }

            InitiateDatabaseForNewUser(user.Id);
            ClaimsIdentity identity = await GetIdentityAsync(user.Id);
            string token = CreateAccessToken(identity);

            return OAuthAccessToken(token, "bearer", user.UserName);
        }

        /// <summary>
        /// Initiate a new todo list for new user
        /// </summary>
        /// <param name="userName"></param>
        internal static void InitiateDatabaseForNewUser(string userName)
        {
            using (TodoDbContext db = new TodoDbContext())
            {
                TodoList todoList = new TodoList();
                todoList.UserId = userName;
                todoList.Title = "My Todo List #1";
                todoList.Todos = new List<TodoItem>();
                db.TodoLists.Add(todoList);
                db.SaveChanges();

                todoList.Todos.Add(new TodoItem()
                {
                    Title = "Todo item #1",
                    TodoListId = todoList.TodoListId,
                    IsDone = false
                });
                todoList.Todos.Add(new TodoItem()
                {
                    Title = "Todo item #2",
                    TodoListId = todoList.TodoListId,
                    IsDone = false
                });
                db.SaveChanges();
            }
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinRequest().Authentication; }
        }

        private IHttpActionResult BadRequest()
        {
            return StatusCode(HttpStatusCode.BadRequest);
        }

        private IHttpActionResult BadRequest(string error)
        {
            return Message(Request.CreateErrorResponse(HttpStatusCode.BadRequest, error));
        }

        private IHttpActionResult BadRequest(string format, params object[] args)
        {
            return BadRequest(String.Format(CultureInfo.CurrentCulture, format, args));
        }

        private IHttpActionResult BadRequest(ModelStateDictionary modelState)
        {
            return Message(Request.CreateErrorResponse(HttpStatusCode.BadRequest, modelState));
        }

        private string CreateAccessToken(ClaimsIdentity identity)
        {
            return Bearer.AccessTokenHandler.Protect(new AuthenticationTicket(identity, new AuthenticationExtra()));
        }

        private string CreateExternalAccessToken(ClaimsIdentity identity, TimeSpan expiresIn)
        {
            Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

            if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer))
            {
                return null;
            }

            ExternalAccessToken token = new ExternalAccessToken
            {
                LoginProvider = providerKeyClaim.Issuer,
                ProviderKey = providerKeyClaim.Value,
                DisplayName = identity.Name,
                Expires = DateTime.UtcNow.Add(expiresIn)
            };

            return ExternalTokenHandler.Protect(token);
        }

        private string GenerateAntiForgeryState()
        {
            const int strengthInBits = 256;
            const int strengthInBytes = strengthInBits / 8;
            byte[] data = new byte[strengthInBytes];

            using (RandomNumberGenerator generator = new RNGCryptoServiceProvider())
            {
                generator.GetBytes(data);
            }

            return Convert.ToBase64String(data);
        }

        private async Task<ClaimsIdentity> GetIdentityAsync(string userId)
        {
            IList<Claim> claims = await AuthenticationManager.GetUserIdentityClaims(userId, new Claim[0]);

            if (claims == null)
            {
                return null;
            }

            return new ClaimsIdentity(claims, Bearer.AuthenticationType, AuthenticationManager.UserNameClaimType,
                AuthenticationManager.RoleClaimType);
        }

        private IHttpActionResult OAuthAccessToken(string accessToken, string tokenType, string userName)
        {
            return new OAuthAccessTokenResult(new OAuthTokenViewModel
            {
                AccessToken = accessToken,
                TokenType = tokenType,
                UserName = userName
            }, this);
        }

        private IHttpActionResult OAuthBadRequest(OAuthAccessTokenError error, string errorDescription = null)
        {
            return new OAuthAccessTokenBadRequestResult(error, errorDescription, this);
        }

        private IHttpActionResult OAuthImplicitError(string redirectUrl, string state,
            OAuthImplicitAccessTokenError error)
        {
            return new OAuthImplicitAccessTokenErrorResult(redirectUrl, error, state, this);
        }

        private IHttpActionResult OK()
        {
            return StatusCode(HttpStatusCode.OK);
        }

        private static bool ValidateRedirectUri(string clientId, string redirectUri)
        {
            if (clientId != "self")
            {
                return false;
            }

            if (RequestExtensions.IsUrlLocalToHost(null, redirectUri))
            {
                return true;
            }

            Uri uri;

            if (Uri.TryCreate(redirectUri, UriKind.Absolute, out uri))
            {
                if (uri.Scheme == "ms-app")
                {
                    return true;
                }
            }

            return false;
        }

        public class ExternalAccessToken
        {
            public string LoginProvider { get; set; }

            public string ProviderKey { get; set; }

            public string DisplayName { get; set; }

            public DateTime Expires { get; set; }

            public bool IsValid
            {
                get
                {
                    return DateTime.UtcNow < Expires;
                }
            }
        }

        private class ExternalAccessTokenHandler : ISecureDataHandler<ExternalAccessToken>
        {
            private const string LoginProvider = "p";
            private const string ProviderKey = "k";
            private const string DisplayName = "d";
            private const string Expires = "e";

            private static readonly ClaimsIdentity emptyIdentity = new ClaimsIdentity(claims: null,
                authenticationType: String.Empty);

            public ExternalAccessTokenHandler(ISecureDataHandler<AuthenticationTicket> innerHandler)
            {
                InnerHandler = innerHandler;
            }

            public ISecureDataHandler<AuthenticationTicket> InnerHandler { get; set; }

            public string Protect(ExternalAccessToken data)
            {
                return InnerHandler.Protect(new AuthenticationTicket(emptyIdentity, Serialize(data)));
            }

            public ExternalAccessToken Unprotect(string protectedText)
            {
                AuthenticationTicket ticket = InnerHandler.Unprotect(protectedText);

                if (ticket == null)
                {
                    return null;
                }

                AuthenticationExtra extra = ticket.Extra;

                if (extra == null)
                {
                    return null;
                }

                return Deserialize(extra);
            }

            private static AuthenticationExtra Serialize(ExternalAccessToken token)
            {
                AuthenticationExtra extra = new AuthenticationExtra();

                if (token == null)
                {
                    return extra;
                }

                extra.Properties[LoginProvider] = token.LoginProvider ?? String.Empty;
                extra.Properties[ProviderKey] = token.ProviderKey ?? String.Empty;
                extra.Properties[DisplayName] = token.DisplayName ?? String.Empty;
                extra.Properties[Expires] = token.Expires.ToString("u", CultureInfo.InvariantCulture);
                return extra;
            }

            private static ExternalAccessToken Deserialize(AuthenticationExtra extra)
            {
                return new ExternalAccessToken
                {
                    LoginProvider = extra.Properties[LoginProvider],
                    ProviderKey = extra.Properties[ProviderKey],
                    DisplayName = extra.Properties[DisplayName],
                    Expires = DateTime.ParseExact(extra.Properties[Expires], "u", CultureInfo.InvariantCulture)
                };
            }
        }

        #endregion
    }
}

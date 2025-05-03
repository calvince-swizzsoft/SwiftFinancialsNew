using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using SwiftFinancials.Apis.Configuration;

namespace SwiftFinancials.Apis.Filters
{
    /// <summary>
    /// Generic Basic Authentication filter that checks for basic authentication
    /// headers and challenges for authentication if no authentication is provided
    /// Sets the Thread Principle with a GenericAuthenticationPrincipal.
    /// 
    /// You can override the OnAuthorize method for custom auth logic that
    /// might be application specific.    
    /// </summary>
    /// <remarks>Always remember that Basic Authentication passes username and passwords
    /// from client to server in plain text, so make sure SSL is used with basic auth
    /// to encode the Authorization header on all requests (not just the login).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class InternetBankingAuthenticationFilter : AuthorizationFilterAttribute
    {
        bool Active = true;

        bool RequireHttps = false;

        string ApiUserName = string.Empty;

        string ApiPassword = string.Empty;

        public InternetBankingAuthenticationFilter()
        {
            var webApiConfigSection = (WebApiConfigSection)ConfigurationManager.GetSection("webApiConfiguration");

            if (webApiConfigSection != null)
            {
                RequireHttps = webApiConfigSection.WebApiSettingsItems.RequireHttps == 1;

                foreach (var settingsItem in webApiConfigSection.WebApiSettingsItems)
                {
                    var webApiSettingsElement = (WebApiSettingsElement)settingsItem;

                    if (webApiSettingsElement != null && webApiSettingsElement.Enabled == 1)
                    {
                        ApiUserName = webApiSettingsElement.InternetBankingApiUsername;

                        ApiPassword = webApiSettingsElement.InternetBankingApiPassword;
                    }
                }
            }
        }

        /// <summary>
        /// Overriden constructor to allow explicit disabling of this
        /// filter's behavior. Pass false to disable (same as no filter
        /// but declarative)
        /// </summary>
        /// <param name="active"></param>
        public InternetBankingAuthenticationFilter(bool active)
            : this()
        {
            Active = active;
        }

        /// <summary>
        /// Override to Web API filter method to handle Basic Auth check
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (Active)
            {
                if (RequireHttps && actionContext.Request.RequestUri.Scheme != Uri.UriSchemeHttps)
                {
                    Challenge(actionContext);
                    return;
                }
                else
                {
                    var identity = ParseAuthorizationHeader(actionContext);
                    if (identity == null)
                    {
                        Challenge(actionContext);
                        return;
                    }

                    if (!OnAuthorizeUser(identity.Name, identity.Password, actionContext))
                    {
                        Challenge(actionContext);
                        return;
                    }

                    var principal = new GenericPrincipal(identity, null);

                    Thread.CurrentPrincipal = principal;

                    // inside of ASP.NET this is also required for some async scenarios
                    if (HttpContext.Current != null)
                        HttpContext.Current.User = principal;

                    base.OnAuthorization(actionContext);
                }
            }
        }

        /// <summary>
        /// Base implementation for user authentication - you probably will
        /// want to override this method for application specific logic.
        /// 
        /// The base implementation merely checks for username and password
        /// present and set the Thread principal.
        /// 
        /// Override this method if you want to customize Authentication
        /// and store user data as needed in a Thread Principle or other
        /// Request specific storage.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        protected virtual bool OnAuthorizeUser(string username, string password, HttpActionContext actionContext)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            return username.Equals(ApiUserName, StringComparison.OrdinalIgnoreCase) && password.Equals(ApiPassword, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses the Authorization header and creates user credentials
        /// </summary>
        /// <param name="actionContext"></param>
        protected virtual InternetBankingAuthenticationIdentity ParseAuthorizationHeader(HttpActionContext actionContext)
        {
            string authHeader = null;
            var auth = actionContext.Request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic")
                authHeader = auth.Parameter;

            if (string.IsNullOrEmpty(authHeader))
                return null;

            authHeader = Encoding.Default.GetString(Convert.FromBase64String(authHeader));

            // find first : as password allows for :
            int idx = authHeader.IndexOf(':');
            if (idx < 0)
                return null;

            string username = authHeader.Substring(0, idx);
            string password = authHeader.Substring(idx + 1);

            return new InternetBankingAuthenticationIdentity(username, password);
        }


        /// <summary>
        /// Send the Authentication Challenge request
        /// </summary>
        /// <param name="message"></param>
        /// <param name="actionContext"></param>
        void Challenge(HttpActionContext actionContext)
        {
            var host = actionContext.Request.RequestUri.DnsSafeHost;
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", string.Format("Basic realm=\"{0}\"", host));
        }
    }

    public class InternetBankingAuthenticationIdentity : GenericIdentity
    {
        public InternetBankingAuthenticationIdentity(string name, string password)
            : base(name, "Basic")
        {
            this.Password = password;
        }

        /// <summary>
        /// Basic Auth Password for custom authentication
        /// </summary>
        public string Password { get; set; }
    }
}
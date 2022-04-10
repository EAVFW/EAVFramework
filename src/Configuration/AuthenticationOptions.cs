using DotNetDevOps.Extensions.EAVFramework.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DotNetDevOps.Extensions.EAVFramework.Configuration
{
    /// <summary>
    /// Configures the login and logout views and behavior.
    /// </summary>
    public class AuthenticationOptions
    {
        /// <summary>
        /// Sets the cookie authentication scheme configured by the host used for interactive users. If not set, the scheme will inferred from the host's default authentication scheme.
        /// This setting is typically used when AddPolicyScheme is used in the host as the default scheme.
        /// </summary>
        public string CookieAuthenticationScheme { get; set; }

        /// <summary>
        /// Sets the cookie lifetime (only effective if the IdentityServer-provided cookie handler is used)
        /// </summary>
        public TimeSpan CookieLifetime { get; set; } = Constants.DefaultCookieTimeSpan;

        /// <summary>
        /// Specified if the cookie should be sliding or not (only effective if the built-in cookie middleware is used)
        /// </summary>
        public bool CookieSlidingExpiration { get; set; } = false;

        /// <summary>
        /// Specifies the SameSite mode for the internal authentication and temp cookie
        /// </summary>
        public SameSiteMode CookieSameSiteMode { get; set; } = SameSiteMode.None;

        /// <summary>
        /// Indicates if user must be authenticated to accept parameters to end session endpoint. Defaults to false.
        /// </summary>
        /// <value>
        /// <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        public bool RequireAuthenticatedUserForSignOutMessage { get; set; } = false;

        public Func<HttpContext, ClaimsPrincipal, List<Claim>,string, string,ValueTask> PopulateAuthenticationClaimsAsync { get; set; } = DefaultPopulateAuthenticationClaimsAsync;

        public Func<HttpContext, ValueTask<string>> GenerateHandleId { get; set; } = DefaultHandleIdGenerator;

        private static ValueTask<string> DefaultHandleIdGenerator(HttpContext httpcontext) => new ValueTask<string>( CryptographyHelpers.CreateCryptographicallySecureGuid().ToString("N"));
     

        public Func<HttpContext, ClaimsPrincipal, List<Claim>,string, string,ValueTask> OnAuthenticatedAsync { get; set; } = DefaultPopulateAuthenticationClaimsAsync;


        static internal ValueTask DefaultPopulateAuthenticationClaimsAsync(HttpContext http, ClaimsPrincipal principal, List<Claim> claims,string provider, string handleid) => default;

        /// <summary>
        /// Gets or sets the name of the cookie used for the check session endpoint.
        /// </summary>
        public string CheckSessionCookieName { get; set; } = Constants.DefaultCheckSessionCookieName;

        /// <summary>
        /// Gets or sets the domain of the cookie used for the check session endpoint. Defaults to null.
        /// </summary>
        public string CheckSessionCookieDomain { get; set; }

        /// <summary>
        /// Gets or sets the SameSite mode of the cookie used for the check session endpoint. Defaults to SameSiteMode.None.
        /// </summary>
        public SameSiteMode CheckSessionCookieSameSiteMode { get; set; } = SameSiteMode.None;

        /// <summary>
        /// If set, will require frame-src CSP headers being emitting on the end session callback endpoint which renders iframes to clients for front-channel signout notification.
        /// </summary>
        public bool RequireCspFrameSrcForSignout { get; set; } = true;
    }

}

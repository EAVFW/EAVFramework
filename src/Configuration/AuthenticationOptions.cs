using EAVFramework.Authentication;
using EAVFramework.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EAVFramework.Configuration
{

    public class PersistTicketRequest
    {
        /// <summary>
        /// The unique id for the signin session, handleid
        /// </summary>
        public Guid HandleId { get; set; }

        /// <summary>
        /// The httpcontext for which the signin sesion is in progress where applicable
        /// </summary>
        public HttpContext HttpContext { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The identity id of the current signin session user if known
        /// </summary>
        public Guid? IdentityId { get; set; }

        /// <summary>
        /// The data to persist
        /// </summary>
        public byte[] Ticket { get; set; }

        /// <summary>
        /// The redirect url to use when the signin session is completed
        /// </summary>
        public string RedirectUrl { get; set; }
        public string AuthProvider { get;  set; }
        public ClaimsPrincipal OwnerIdentity { get;  set; }
    }
    public class TicketInformation
    {
        /// <summary>
        /// The data to persist
        /// </summary>
        public byte[] Ticket { get; set; }

        /// <summary>
        /// The redirect url to use when the signin session is completed
        /// </summary>
        public string RedirectUrl { get; set; }
        public Guid IdentityId { get;  set; }
    }

    public class UserDiscoveryRequest
    {
        /// <summary>
        /// The httpcontext for which the signin sesion is in progress where applicable
        /// </summary>
        public HttpContext HttpContext { get; set; }

        public IServiceProvider ServiceProvider { get; set; }   

        public string Email { get; set; }

    }
    public class EmailDiscoveryRequest
    {
        /// <summary>
        /// The httpcontext for which the signin sesion is in progress where applicable
        /// </summary>
        public HttpContext HttpContext { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public Guid IdentityId { get; set; }

    }

    public class UnPersistTicketReuqest
    {
        public Guid HandleId { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public HttpContext HttpContext { get; set; }
    }

    /// <summary>
    /// Configures the login and logout views and behavior.
    /// </summary>
    public class AuthenticationOptions
    {

        ///// <summary>
        ///// Persist the ticket information
        ///// </summary>
        //public Func<PersistTicketRequest, Task> PersistTicketAsync { get; set; }
        ///// <summary>
        ///// Persist the ticket information
        ///// </summary>
        //public Func<UnPersistTicketReuqest, Task<TicketInformation>> UnPersistTicketAsync { get; set; }


        /// <summary>
        /// Function for taking an email address and looking up the unique ID for the corresponding user.
        /// If this function returns null, the OnNotFound delegate will be executed.
        /// </summary>
        public Func<UserDiscoveryRequest, Task<Guid?>> FindIdentity { get; set; }

   
        public Func<EmailDiscoveryRequest, Task<string>> FindEmailFromIdentity { get; set; }


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

        public Func<IServiceProvider, ValueTask<Guid>> GenerateHandleId { get; set; } = DefaultHandleIdGenerator;

        private static ValueTask<Guid> DefaultHandleIdGenerator(IServiceProvider serviceProvider) => new ValueTask<Guid>( CryptographyHelpers.CreateCryptographicallySecureGuid());
     

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
        public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.Always;

        public bool EnableEasyAuth { get; set; } = true;
    }

}

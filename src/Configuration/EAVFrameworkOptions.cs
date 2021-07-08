using DotNetDevOps.Extensions.EAVFramework.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace DotNetDevOps.Extensions.EAVFramework.Configuration
{
    internal class ConfigureInternalCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        private readonly EAVFrameworkOptions _options;

        public ConfigureInternalCookieOptions(EAVFrameworkOptions options)
        {
            _options = options;
        }

        public void Configure(CookieAuthenticationOptions options)
        {
        }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            if (name == Constants.DefaultCookieAuthenticationScheme)
            {
                options.SlidingExpiration = _options.Authentication.CookieSlidingExpiration;
                options.ExpireTimeSpan = _options.Authentication.CookieLifetime;
                options.Cookie.Name = Constants.DefaultCookieAuthenticationScheme;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = _options.Authentication.CookieSameSiteMode;

                options.LoginPath = ExtractLocalUrl(_options.UserInteraction.LoginUrl);
                options.LogoutPath = ExtractLocalUrl(_options.UserInteraction.LogoutUrl);
                if (_options.UserInteraction.LoginReturnUrlParameter != null)
                {
                    options.ReturnUrlParameter = _options.UserInteraction.LoginReturnUrlParameter;
                }
            }

            if (name == Constants.ExternalCookieAuthenticationScheme)
            {
               
                options.Cookie.Name = Constants.ExternalCookieAuthenticationScheme;
                options.Cookie.IsEssential = true;
                // https://github.com/IdentityServer/IdentityServer4/issues/2595
                // need to set None because iOS 12 safari considers the POST back to the client from the 
                // IdP as not safe, so cookies issued from response (with lax) then should not be honored.
                // so we need to make those cookies issued without same-site, thus the browser will
                // hold onto them and send on the next redirect to the callback page.
                // see: https://brockallen.com/2019/01/11/same-site-cookies-asp-net-core-and-external-authentication-providers/
                options.Cookie.SameSite = _options.Authentication.CookieSameSiteMode;
            }
        }

        private static string ExtractLocalUrl(string url)
        {
            if (url.IsLocalUrl())
            {
                if (url.StartsWith("~/"))
                {
                    url = url.Substring(1);
                }

                return url;
            }

            return null;
        }
    }

    internal class PostConfigureInternalCookieOptions : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly EAVFrameworkOptions _options;
        private readonly IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> _authOptions;
        private readonly ILogger _logger;

        public PostConfigureInternalCookieOptions(
            EAVFrameworkOptions options,
            IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> authOptions,
            ILoggerFactory loggerFactory)
        {
            _options = options;
            _authOptions = authOptions;
            _logger = loggerFactory.CreateLogger("DotNetDevOps.Extentions.EAVFramework.Startup");
        }

        public void PostConfigure(string name, CookieAuthenticationOptions options)
        {
            var scheme = _options.Authentication.CookieAuthenticationScheme ??
                _authOptions.Value.DefaultAuthenticateScheme ??
                _authOptions.Value.DefaultScheme;

            if (name == scheme)
            {
                _options.UserInteraction.LoginUrl = _options.UserInteraction.LoginUrl ?? options.LoginPath;
                _options.UserInteraction.LoginReturnUrlParameter = _options.UserInteraction.LoginReturnUrlParameter ?? options.ReturnUrlParameter;
                _options.UserInteraction.LogoutUrl = _options.UserInteraction.LogoutUrl ?? options.LogoutPath;

                _logger.LogDebug("Login Url: {url}", _options.UserInteraction.LoginUrl);
                _logger.LogDebug("Login Return Url Parameter: {param}", _options.UserInteraction.LoginReturnUrlParameter);
                _logger.LogDebug("Logout Url: {url}", _options.UserInteraction.LogoutUrl);

                _logger.LogDebug("ConsentUrl Url: {url}", _options.UserInteraction.ConsentUrl);
                _logger.LogDebug("Consent Return Url Parameter: {param}", _options.UserInteraction.ConsentReturnUrlParameter);

                _logger.LogDebug("Error Url: {url}", _options.UserInteraction.ErrorUrl);
                _logger.LogDebug("Error Id Parameter: {param}", _options.UserInteraction.ErrorIdParameter);
            }
        }
    }


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

    

    public class EAVFrameworkOptions
    {

        

        /// <summary>
        /// Gets or sets the events options.
        /// </summary>
        /// <value>
        /// The events options.
        /// </value>
        public EventsOptions Events { get; set; } = new EventsOptions();


        /// <summary>
        /// Gets or sets the endpoint configuration.
        /// </summary>
        /// <value>
        /// The endpoints configuration.
        /// </value>
        public EndpointsOptions Endpoints { get; set; } = new EndpointsOptions();

        public string RoutePrefix { get; set; } = "/api";

        /// <summary>
        /// Gets or sets the authentication options.
        /// </summary>
        /// <value>
        /// The authentication options.
        /// </value>
        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();

        /// <summary>
        /// Gets or sets the options for the user interaction.
        /// </summary>
        /// <value>
        /// The user interaction options.
        /// </value>
        public UserInteractionOptions UserInteraction { get; set; } = new UserInteractionOptions();
    }

    /// <summary>
    /// Options for aspects of the user interface.
    /// </summary>
    public class UserInteractionOptions
    {
        /// <summary>
        /// Gets or sets the login URL. If a local URL, the value must start with a leading slash.
        /// </summary>
        /// <value>
        /// The login URL.
        /// </value>
        public string LoginUrl { get; set; } //= Constants.UIConstants.DefaultRoutePaths.Login.EnsureLeadingSlash();

        /// <summary>
        /// Gets or sets the login return URL parameter.
        /// </summary>
        /// <value>
        /// The login return URL parameter.
        /// </value>
        public string LoginReturnUrlParameter { get; set; } //= Constants.UIConstants.DefaultRoutePathParams.Login;

        /// <summary>
        /// Gets or sets the logout URL. If a local URL, the value must start with a leading slash.
        /// </summary>
        /// <value>
        /// The logout URL.
        /// </value>
        public string LogoutUrl { get; set; } //= Constants.UIConstants.DefaultRoutePaths.Logout.EnsureLeadingSlash();

        /// <summary>
        /// Gets or sets the logout identifier parameter.
        /// </summary>
        /// <value>
        /// The logout identifier parameter.
        /// </value>
        public string LogoutIdParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Logout;

        /// <summary>
        /// Gets or sets the consent URL. If a local URL, the value must start with a leading slash.
        /// </summary>
        /// <value>
        /// The consent URL.
        /// </value>
        public string ConsentUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Consent.EnsureLeadingSlash();

        /// <summary>
        /// Gets or sets the consent return URL parameter.
        /// </summary>
        /// <value>
        /// The consent return URL parameter.
        /// </value>
        public string ConsentReturnUrlParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Consent;

        /// <summary>
        /// Gets or sets the error URL. If a local URL, the value must start with a leading slash.
        /// </summary>
        /// <value>
        /// The error URL.
        /// </value>
        public string ErrorUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.Error.EnsureLeadingSlash();

        /// <summary>
        /// Gets or sets the error identifier parameter.
        /// </summary>
        /// <value>
        /// The error identifier parameter.
        /// </value>
        public string ErrorIdParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Error;

        /// <summary>
        /// Gets or sets the custom redirect return URL parameter.
        /// </summary>
        /// <value>
        /// The custom redirect return URL parameter.
        /// </value>
        public string CustomRedirectReturnUrlParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.Custom;

        /// <summary>
        /// Gets or sets the cookie message threshold. This limits how many cookies are created, and older ones will be purged.
        /// </summary>
        /// <value>
        /// The cookie message threshold.
        /// </value>
        public int CookieMessageThreshold { get; set; } = Constants.UIConstants.CookieMessageThreshold;

        /// <summary>
        /// Gets or sets the device verification URL.  If a local URL, the value must start with a leading slash.
        /// </summary>
        /// <value>
        /// The device verification URL.
        /// </value>
        public string DeviceVerificationUrl { get; set; } = Constants.UIConstants.DefaultRoutePaths.DeviceVerification;

        /// <summary>
        /// Gets or sets the device verification user code paramater.
        /// </summary>
        /// <value>
        /// The device verification user code parameter.
        /// </value>
        public string DeviceVerificationUserCodeParameter { get; set; } = Constants.UIConstants.DefaultRoutePathParams.UserCode;

        /// <summary>
        /// Flag that allows return URL validation to accept full URL that includes the IdentityServer origin. Defaults to false.
        /// </summary>
        public bool AllowOriginInReturnUrl { get; set; }
    }


    /// <summary>
    /// IdentityServer builder Interface
    /// </summary>
    public interface IEAVFrameworkBuilder
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        IServiceCollection Services { get; }
    }

    /// <summary>
    /// IdentityServer helper class for DI configuration
    /// </summary>
    public class EAVFrameworkBuilder : IEAVFrameworkBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public EAVFrameworkBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        public IServiceCollection Services { get; }
    }

    public class AuthenticatedEAVFrameworkBuilder //: EAVFrameworkBuilder
    {
        private readonly IEAVFrameworkBuilder _eavFrameworkBuilder;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public AuthenticatedEAVFrameworkBuilder(IEAVFrameworkBuilder eavFrameworkBuilder)
        {
            _eavFrameworkBuilder = eavFrameworkBuilder;
        }

        public IServiceCollection Services => _eavFrameworkBuilder.Services;
    }

}

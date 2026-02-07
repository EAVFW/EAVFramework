using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAVFramework.Configuration
{
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

}

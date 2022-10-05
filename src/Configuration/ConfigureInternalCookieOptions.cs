using EAVFramework.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace EAVFramework.Configuration
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

}

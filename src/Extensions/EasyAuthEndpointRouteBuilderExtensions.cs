using System;
using System.Linq;
using System.Security.Claims;
using DotNetDevOps.Extensions.EAVFramework.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using DotNetDevOps.Extensions.EAVFramework.Configuration;

namespace DotNetDevOps.Extensions.EAVFramework.Extensions
{
    public static class EasyAuthEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder AddEasyAuth(
            this IEndpointRouteBuilder endpoints,
            AuthenticationProperties authenticationProperties = null)
        {
            var authProps = authenticationProperties ?? new AuthenticationProperties();
            return MapAuthEndpoints(endpoints, authProps);
        }

        private static IEndpointConventionBuilder MapAuthEndpoints(
            IEndpointRouteBuilder endpoints,
            AuthenticationProperties authProps)
        {
            var sp = endpoints.ServiceProvider;
            var options = endpoints.ServiceProvider.GetService<EAVFrameworkOptions>();
            var authProviders = sp.GetServices<IEasyAuthProvider>().ToList();

            foreach (var auth in authProviders.Where(x => x.AutoGenerateRoutes))
            {
                //https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization
                var authUrl = $"/.auth/login/{auth.AuthenticationName}";

                endpoints.MapGet(authUrl, async (httpcontext) =>
                {
                    var handleId = CryptographyHelpers.CreateCryptographicallySecureGuid().ToString("N");
                    var baseUrl = $"{new Uri(httpcontext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority)}";
                    var callbackUrl = $"{baseUrl}/.auth/login/{auth.AuthenticationName}/callback?token={handleId}";

                    var requestDelegate = auth.OnAuthenticate(handleId, callbackUrl);
                    await requestDelegate(httpcontext);
                }).WithMetadata(new AllowAnonymousAttribute());

                endpoints.MapMethods(
                    $"{authUrl}/callback",
                    new[] { auth.CallbackHttpMethod.ToString().ToUpperInvariant() },
                    async (httpcontext) =>
                    {
                        var handleId = httpcontext.Request.Query["token"].FirstOrDefault();
                        var (claimsPrincipal, redirectUri) = await auth.OnCallback(handleId, httpcontext);

                        await httpcontext.SignInAsync(Constants.ExternalCookieAuthenticationScheme,
                            claimsPrincipal, new AuthenticationProperties(
                                new Dictionary<string, string>
                                {
                                    ["handleId"] = handleId,
                                    ["callbackUrl"] = redirectUri,
                                    ["schema"] = auth.AuthenticationName
                                }));

                        httpcontext.Response.Redirect("/account/login/callback");
                    }).WithMetadata(new AllowAnonymousAttribute());
            }

            endpoints.MapGet(Constants.UIConstants.DefaultRoutePaths.Login,
                async r =>
                {
                    var hasReturnUrl = r.Request.Query.TryGetValue("returnUrl", out StringValues returnUrlHeaders);
                    var redirect = hasReturnUrl ? returnUrlHeaders.FirstOrDefault() : "/";
                    // store the redirect in a cookie
                    // r.Response.Cookies.Append(Constants.DefaultLoginRedirectCookie, redirect);

                    var hasIdp = r.Request.Query.TryGetValue("idp", out StringValues idp);
                    if (hasIdp)
                    {
                        // if idp query param given, pass on directly to idp
                        r.Response.Redirect(
                            $"/.auth/login/{idp.FirstOrDefault()}?redirectUri={new Uri(r.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority) + Constants.UIConstants.DefaultRoutePaths.LoginCallback}");
                    }
                    else
                    {
                        var webRootPath = r.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                        // otherwise, serve this:
                        await r.Response.SendFileAsync($"{webRootPath}/account/login/index.html");
                    }
                }).WithMetadata(new AllowAnonymousAttribute());

            //The idp must set the expected claims at least.
            //when signed in, check cookie for the redirect uri
            endpoints.MapGet(Constants.UIConstants.DefaultRoutePaths.LoginCallback, async httpcontext =>
            {
                //http://docs.identityserver.io/en/latest/topics/signin_external_providers.html

                var result = await httpcontext.AuthenticateAsync(Constants.ExternalCookieAuthenticationScheme);
                if (result?.Succeeded != true)
                {
                    throw new Exception("External authentication error");
                }

                // retrieve claims of the external user
                var externalUser = result.Principal;
                if (externalUser == null)
                {
                    throw new Exception("External authentication error");
                }

                // retrieve claims of the external user
                var claims = externalUser.Claims.ToList();

                // try to determine the unique id of the external user - the most common claim type for that are the sub claim and the NameIdentifier
                // depending on the external provider, some other claim type might be used
                var userIdClaim = claims.FirstOrDefault(x => x.Type == "sub") ??
                                  claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier) ??
                                  claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);

                if (userIdClaim == null)
                {
                    throw new Exception("Unknown userid");
                }

                var externalUserId = userIdClaim.Value;
                var externalProvider = userIdClaim.Issuer;

                //  var external = await httpcontext.AuthenticateAsync(Constants.ExternalCookieAuthenticationScheme);
                //var external = await httpcontext.AuthenticateAsync(loginflow.Properties.Items["schema"]);

                await options.Authentication.PopulateAuthenticationClaimsAsync(httpcontext, externalUser, claims);

                await httpcontext.SignInAsync(Constants.DefaultCookieAuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, Constants.DefaultCookieAuthenticationScheme)),
                    new AuthenticationProperties());

                await httpcontext.SignOutAsync(Constants.ExternalCookieAuthenticationScheme);


                //TODO make better way to get that return url
                if (httpcontext.Request.Cookies.TryGetValue(Constants.DefaultLoginRedirectCookie,
                    out string redirectUri))
                {
                    httpcontext.Response.Cookies.Delete(Constants.DefaultLoginRedirectCookie);
                    httpcontext.Response.Redirect(redirectUri);
                }
                else
                {
                    var baseUrl =
                        $"{new Uri(httpcontext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority)}";
                    httpcontext.Response.Redirect(baseUrl);
                }
            }).WithMetadata(new AllowAnonymousAttribute());

            return endpoints.MapGet("/.auth/me", async context =>
            {
                // for now just take first authenticator
                var auth = await context.AuthenticateAsync(Constants.DefaultCookieAuthenticationScheme);

                //handle auth failures ect

                await context.Response.WriteJsonAsync(auth.Principal.Claims.GroupBy(k => k.Type)
                    .ToDictionary(k => k.Key,
                        v => v.Skip(1).Any()
                            ? v.Select(c => c.Value).ToArray()
                            : v.Select(c => c.Value).First() as object));
            });
        }
    }
}
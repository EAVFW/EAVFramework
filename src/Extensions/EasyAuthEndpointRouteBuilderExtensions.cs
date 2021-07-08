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
            var authProviders = sp.GetServices<IEasyAuthProvider>().ToList();
            foreach (var auth in authProviders)
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

                endpoints.MapGet($"{authUrl}/callback", async (httpcontext) =>
                {
                    var handleId = httpcontext.Request.Query["token"].First();
                    var (claimsIdentity, redirectUri) = await auth.OnCallback(handleId, httpcontext);

                    await httpcontext.SignInAsync(
                        auth.AuthenticationName,
                        new ClaimsPrincipal(claimsIdentity),
                        authProps);
                    httpcontext.Response.Redirect(redirectUri ?? "/account/login/callback");
                }).WithMetadata(new AllowAnonymousAttribute());
            }

            endpoints.MapGet(Constants.UIConstants.DefaultRoutePaths.Login,
                async r =>
                {
                    var webRootPath = r.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                    var hasRedirect = r.Request.Query.TryGetValue("returnUrl", out StringValues redirectHeader);
                    var redirect = hasRedirect ? redirectHeader.FirstOrDefault() : webRootPath;
                    // store the redirect in a cookie
                    r.Response.Cookies.Append(Constants.DefaultLoginRedirectCookie, redirect);

                    var hasIdp = r.Request.Query.TryGetValue("idp", out StringValues idp);
                    if (hasIdp)
                    {
                        // if idp query param given, pass on directly to idp
                        r.Response.Redirect($"/.auth/login/{idp.FirstOrDefault()}?redirectUri={new Uri(r.Request.GetDisplayUrl()).GetLeftPart( UriPartial.Authority)+ Constants.UIConstants.DefaultRoutePaths.LoginCallback}");
                    }
                    else
                    {
                        // otherwise, serve this:
                        await r.Response.SendFileAsync($"{webRootPath}/account/login/index.html");
                    }
                }).WithMetadata(new AllowAnonymousAttribute());

            //The idp must set the expected claims at least.
            //when signed in, check cookie for the redirect uri
            endpoints.MapGet(Constants.UIConstants.DefaultRoutePaths.LoginCallback, async httpcontext =>
            {
                if (httpcontext.Request.Cookies.TryGetValue(Constants.DefaultLoginRedirectCookie, out string redirectUri))
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
            });

            return endpoints.MapGet("/account/profile", async context =>
            {
                // for now just take first authenticator
                var auth = await context.AuthenticateAsync(authProviders.FirstOrDefault().AuthenticationName);

                //handle auth failures ect

                await context.Response.WriteJsonAsync(auth.Principal.Claims.ToDictionary(k => k.Type, v => v.Value));
            });
        }
    }
}

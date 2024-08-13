using System;
using System.Linq;
using System.Security.Claims;
using EAVFramework.Authentication;
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
using EAVFramework.Configuration;
using System.Net;
using EAVFramework.OpenTelemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Sprache;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.Cookies;


namespace EAVFramework.Extensions
{
    
   
    public class OnAuthenticateRequest
    {
        public string CallbackUrl { get; set; } 
        public HttpContext HttpContext { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public Guid HandleId { get; set; }
        public Guid? IdentityId { get;  set; }
        
        public Configuration.AuthenticationOptions Options { get; set; }
        public string Email { get;  set; }
    }
    public class OnCallbackRequest
    {
        public HttpContext HttpContext { get; set; }
        public Configuration.AuthenticationOptions Options { get; set; }
        public Guid HandleId { get;  set; }
       
        public string RedirectUri { get;  set; }
        public Dictionary<string, StringValues> Ticket { get;  set; }
        public Guid? IdentityId { get;  set; }
        public Dictionary<string, StringValues> Props { get;  set; }
    }


    public class LinkResult
    {
        public Guid HandleId { get; set; }
        public string Link { get; set; }
        public bool IsNew { get;  set; }
    }

    public interface IPasswordLessLinkGenerator
    {
        Task<LinkResult> GenerateLink(Guid identityId, string state, string targetUrlOrPath, Guid handleId = default);
    }
    public class PasswordLessLinkGenerator<TContext,TSignin> : IPasswordLessLinkGenerator
          where TContext : DynamicContext
         where TSignin : DynamicEntity, ISigninRecord, new()

    {
        private readonly IOptions<EAVFrameworkOptions> _option;
        private readonly IEAVFrameworkTicketStore<TContext, TSignin> _ticketStore;
        private readonly IServiceProvider _serviceProvider;

        public PasswordLessLinkGenerator(IOptions<EAVFrameworkOptions> option,
            IEAVFrameworkTicketStore<TContext, TSignin> ticketStore,
            IServiceProvider serviceProvider)
        {
            _option = option;
            _ticketStore = ticketStore;
            _serviceProvider = serviceProvider;
        }
        public async Task<LinkResult> GenerateLink(Guid identityId, string state, string targetUrlOrPath, Guid handleId=default)
        {
            if (handleId != default)
            {
                var old = await _ticketStore.GetTicketInfoAsync(new UnPersistTicketReuqest
                {
                    HandleId = handleId,
                    ServiceProvider = _serviceProvider
                });

                if (old != null)
                {
                    return new LinkResult { Link = CreateCallabckUrl(handleId), HandleId = handleId, IsNew = false };
                }
            }


            if (handleId == default)
            {
                handleId = await _option.Value.Authentication.GenerateHandleId(_serviceProvider);
            }

            var ticket = CryptographyHelpers.Encrypt(handleId.ToString("N").Sha512(), handleId.ToString("N").Sha1(),
                           Encoding.UTF8.GetBytes(state));


            await _ticketStore.PersistTicketAsync(new PersistTicketRequest
            {
                HandleId = handleId,
                //  HttpContext = httpcontext,
                IdentityId = identityId == default ? null : identityId,
                Ticket = ticket,
                RedirectUrl = targetUrlOrPath,
                ServiceProvider = _serviceProvider,
                AuthProvider = "passwordless",
                OwnerIdentity = _option.Value.SystemAdministratorIdentity
            });

          

            return new LinkResult { Link = CreateCallabckUrl(handleId), HandleId = handleId, IsNew = true };
        }

        private string CreateCallabckUrl(Guid handleId)
        {
            var baseUrl = _option.Value.Host?.TrimEnd('/');
            var callbackUrl = $"{baseUrl}{_option.Value.PathBase?.TrimEnd('/')}/.auth/login/passwordless/callback?token={handleId.ToString("N")}";
            return callbackUrl;
        }
    }

    /// <summary>
    /// Redirect URL is the url within the application that the user initiated signin from.
    /// Callback URL is the url that the external provider will redirect to after signin.
    /// </summary>

    public static class EasyAuthEndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder AddEasyAuth(
            this IEndpointRouteBuilder endpoints,
            AuthenticationProperties authenticationProperties = null)
          //  where TIdentity : DynamicEntity
        {
            var authProps = authenticationProperties ?? new AuthenticationProperties();
            return MapAuthEndpoints(endpoints, authProps);
        }

        private static IEndpointRouteBuilder MapAuthEndpoints(
            IEndpointRouteBuilder endpoints,
            AuthenticationProperties authProps)
        {
            var sp = endpoints.ServiceProvider;
            var metrics = sp.GetService<EAVMetrics>();
            var options = endpoints.ServiceProvider.GetService<EAVFrameworkOptions>();
            using var scope = sp.CreateScope();
            var authProviders = scope.ServiceProvider.GetServices<IEasyAuthProvider>().ToList();
            var loggerFactory = sp.GetService<ILoggerFactory>();

            foreach (var auth in authProviders.Where(x => x.AutoGenerateRoutes))
            {
                //https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization
                var authUrl = $"/.auth/login/{auth.AuthenticationName}";

                endpoints.MapGet(authUrl, async (HttpContext httpcontext, [FromServices] IEAVFrameworkTicketStore ticketStore,[FromQuery] string redirectUri, [FromQuery] string email) =>
                {
                    var logger = loggerFactory.CreateLogger($"EAVFW.Auth.{auth.AuthenticationName}");
                    
                    metrics?.StartSignup(auth.AuthenticationName);

                    var handleId = await options.Authentication.GenerateHandleId(httpcontext.RequestServices);

                    var identityId = await options.Authentication.FindIdentity(new UserDiscoveryRequest { Email = email,  HttpContext = httpcontext, ServiceProvider = httpcontext.RequestServices });

                    var ticket = CryptographyHelpers.Encrypt(handleId.ToString("N").Sha512(), handleId.ToString("N").Sha1(),
                            Encoding.UTF8.GetBytes( httpcontext.Request.QueryString.Value?.TrimStart('?')));

                    await ticketStore.PersistTicketAsync(new PersistTicketRequest
                    {
                        HandleId = handleId,
                        HttpContext = httpcontext,
                        IdentityId = identityId == default ? null : identityId,
                        Ticket = ticket,
                        RedirectUrl = redirectUri,
                        ServiceProvider = httpcontext.RequestServices,
                        AuthProvider = auth.AuthenticationName,
                        OwnerIdentity = options.SystemAdministratorIdentity
                    });


                    var baseUrl = $"{new Uri(httpcontext.Request.GetDisplayUrl()).GetLeftPart(UriPartial.Authority)}";
                    var callbackUrl = $"{baseUrl}{httpcontext.Request.PathBase}/.auth/login/{auth.AuthenticationName}/callback?token={handleId.ToString("N")}";

                    var result = await auth.OnAuthenticate(new OnAuthenticateRequest
                    {
                        Options = options.Authentication,
                        ServiceProvider = httpcontext.RequestServices,
                         HttpContext = httpcontext, 
                        CallbackUrl = callbackUrl,
                        HandleId = handleId,
                        IdentityId = identityId,
                        Email = email,
                    });


                    if (!result.Success)
                    {
                       

                        var redirectUrl = callbackUrl + $"{(callbackUrl.Contains('?') ? "&" : "?")}error={result.ErrorCode}&error_message={result.ErrorMessage}&error_subcode={result.ErrorSubCode}";

                        logger.LogWarning("Login Failed, {errorMessage}, redirecting to {redirectUrl}", result.ErrorMessage, redirectUrl);

                        httpcontext.Response.Redirect(redirectUrl);
                    }

                  //  await requestDelegate(httpcontext);
                }).WithMetadata(new AllowAnonymousAttribute());

                endpoints.MapMethods(
                    $"{authUrl}/callback",
                    new[] { auth.CallbackHttpMethod.ToString().ToUpperInvariant() },
                    async (HttpContext httpcontext, [FromServices] IEAVFrameworkTicketStore ticketStore, [FromQuery] string error, [FromQuery] string error_message, [FromQuery]string error_subcode) =>
                    {

                        if (!string.IsNullOrEmpty(error))
                        {

                            httpcontext.Response.Redirect(
                                $"{httpcontext.Request.PathBase}/account/login/callback?provider={auth.AuthenticationName}&error={error}&error_message={error_message}&error_subcode={error_subcode}");


                            return;
                        }

                        var request = new OnCallbackRequest{
                            HttpContext = httpcontext,
                            Options = options.Authentication,
                            Props = new Dictionary<string, StringValues>( httpcontext.Request.Query, StringComparer.OrdinalIgnoreCase)
                        };

                        await auth.PopulateCallbackRequest(request);
                        
                        if(request.HandleId != default)
                        {
                            var ticketinfo = await ticketStore.GetTicketInfoAsync(new UnPersistTicketReuqest
                            {
                                HandleId = request.HandleId,
                                ServiceProvider = httpcontext.RequestServices,
                                HttpContext = httpcontext
                            });

                            request.Ticket = QueryHelpers.ParseNullableQuery
                                 (Encoding.UTF8.GetString(CryptographyHelpers.Decrypt(request.HandleId.ToString("N").Sha512(), request.HandleId.ToString("N").Sha1(), ticketinfo.Ticket)));
                            request.IdentityId = ticketinfo.IdentityId;
                            request.RedirectUri = ticketinfo.RedirectUrl;


                        }
                         
                        var result = await auth.OnCallback(request);

                        if (!result.Success)
                        {

                            httpcontext.Response.Redirect(
                                $"{httpcontext.Request.PathBase}/account/login/callback?provider={auth.AuthenticationName}&error={result.ErrorCode}&error_message={result.ErrorMessage}&error_subcode={result.ErrorSubCode}");

                           
                            return;
                        }

                      
                        await httpcontext.SignInAsync(Constants.ExternalCookieAuthenticationScheme,
                            result.Principal, new AuthenticationProperties(
                                
                                new Dictionary<string, string>
                                {
                                    ["handleId"] = request.HandleId.ToString(),
                                    ["callbackUrl"] = request.RedirectUri,
                                    ["schema"] = auth.AuthenticationName
                                })
                            { 
                                 RedirectUri = request.RedirectUri
                            });

                        httpcontext.Response.Redirect($"{httpcontext.Request.PathBase}/account/login/callback?provider{auth.AuthenticationName}");

                     


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
            endpoints.MapGet(Constants.UIConstants.DefaultRoutePaths.LoginCallback,
                async (HttpContext httpcontext, [FromQuery] string error, [FromQuery] string provider) =>
            {
                //http://docs.identityserver.io/en/latest/topics/signin_external_providers.html

                if (!string.IsNullOrEmpty(error))
                {
                    metrics?.SigninFailed(provider);
                    throw new Exception("External authentication error");
                }

                var result = await httpcontext.AuthenticateAsync(Constants.ExternalCookieAuthenticationScheme);
                if (result?.Succeeded != true)
                {
                    metrics?.SigninFailed(provider);
                    throw new Exception("External authentication error");
                }

                // retrieve claims of the external user
                var externalUser = result.Principal;
                if (externalUser == null)
                {
                    metrics?.SigninFailed(provider);
                    throw new Exception("External authentication error");
                }

                var handleId = string.Empty;
                if (result.Properties.Items.ContainsKey("handleId"))
                {
                    handleId=result.Properties.Items["handleId"];
                }

                

                if (result.Properties.Items.ContainsKey("schema"))
                {
                    provider=result.Properties.Items["schema"];
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
                 
                await options.Authentication.PopulateAuthenticationClaimsAsync(httpcontext, externalUser, claims, provider,  handleId);

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Constants.DefaultCookieAuthenticationScheme));
                await httpcontext.SignInAsync(Constants.DefaultCookieAuthenticationScheme, principal,
                    new AuthenticationProperties());

                await options.Authentication.OnAuthenticatedAsync(httpcontext, principal, claims, provider, handleId);

                await httpcontext.SignOutAsync(Constants.ExternalCookieAuthenticationScheme);

                metrics?.SigninSuccess(provider);

                if (!string.IsNullOrWhiteSpace(result.Properties.RedirectUri))
                {
                 
                    httpcontext.Response.Redirect(result.Properties.RedirectUri);
                }
                else 
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

            endpoints.MapGet("/.auth/me", async context =>
            {
                // for now just take first authenticator
                var auth = await context.AuthenticateAsync(Constants.DefaultCookieAuthenticationScheme);
                
                if (auth.Succeeded)
                {

                    await context.Response.WriteJsonAsync(auth.Principal.Claims.GroupBy(k => k.Type).ToDictionary(k => k.Key, v => v.Skip(1).Any() ? v.Select(c => c.Value).ToArray() : v.Select(c => c.Value).First() as object));

                }
                else
                {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteJsonAsync(new { error = "Not Authenticated"});

                }
            }).WithMetadata(new AllowAnonymousAttribute());

            endpoints.MapGet("/.auth/logout", async httpcontext =>
            {
                await httpcontext.SignOutAsync(Constants.DefaultCookieAuthenticationScheme);
                var impersonator = await httpcontext.AuthenticateAsync("eavfw.impersonator");
                if (impersonator.Succeeded)
                {
                    await httpcontext.SignInAsync("eavfw",
                    new ClaimsPrincipal(new ClaimsIdentity(impersonator.Principal.Claims, "eavfw")),
                    new AuthenticationProperties());

                    await httpcontext.SignOutAsync("eavfw.impersonator");
                }

                httpcontext.Response.Redirect(httpcontext.Request.Query["post_logout_redirect_uri"]);
            });



                return endpoints;
        }
    }
}
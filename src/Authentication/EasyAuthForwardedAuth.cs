﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public class EasyAuthForwardedAuth : AuthenticationHandler<EasyAuthForwardedAuthOptions>
    {
        public EasyAuthForwardedAuth(IOptionsMonitor<EasyAuthForwardedAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var hasHeader = Context.Request.Headers.ContainsKey(Options.HeaderName);

            if (!hasHeader)
            {
                this.Logger.LogWarning("No EasyAuth UserID provided");
            }
            
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity(
                        (hasHeader
                            ? new[]
                            {
                                new Claim("sub", Context.Request.Headers[Options.HeaderName])
                            }
                            : new Claim[0]).Concat(Context.Request.Headers.Where(h => h.Key.StartsWith(Options.HeaderPrefix))
                            .Select(k => new Claim(k.Key, k.Value))),
                        Scheme.Name)), Scheme.Name)));
        }
    }
 
}

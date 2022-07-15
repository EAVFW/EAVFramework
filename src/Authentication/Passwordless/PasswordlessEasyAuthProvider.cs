using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using DotNetDevOps.Extensions.EAVFramework.Extensions;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication.Passwordless
{
    public class PasswordlessEasyAuthProvider : IEasyAuthProvider
    {
      
        private readonly SmtpClient _smtp;
        private readonly IOptions<PasswordlessEasyAuthOptions> _options;

        public PasswordlessEasyAuthProvider() { }

        public PasswordlessEasyAuthProvider(
            SmtpClient smtpClient,
            IOptions<PasswordlessEasyAuthOptions> options)
        {
         
            _smtp = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string AuthenticationName => "passwordless";
        public HttpMethod CallbackHttpMethod => HttpMethod.Get;
        public bool AutoGenerateRoutes { get; set; } = true;

        public async Task OnAuthenticate(HttpContext httpcontext, string handleId, string callbackUrl)
        {
           // return async (httpcontext) =>
            {

                var email = httpcontext.Request.Query["email"].FirstOrDefault();
                var redirectUri = httpcontext.Request.Query["redirectUri"].FirstOrDefault();

                var user = await _options.Value.FetchUserIdByEmailAsync(httpcontext,httpcontext.RequestServices, email);

                if (user == null)
                {
                    httpcontext.Response.Redirect(callbackUrl  + $"{(callbackUrl.Contains('?')?"&":"?")}error=access_denied&error_subcode=user_not_found");
                    return;
                }

                var ticket = CryptographyHelpers.Encrypt(handleId.Sha512(), handleId.Sha1(),
                    Encoding.UTF8.GetBytes($"sub={user}&email={email}"));

                await _options.Value.PersistTicketAsync(httpcontext, user, handleId,ticket, redirectUri);

                var options = JToken.FromObject(new
                {
                    unique_args = new Dictionary<string, string>
                    {
                        ["email_id"] = handleId.URLSafeHash(),
                    },
                    filters = new
                    {
                        opentrack = new { settings = new { enable = 0 } },
                        clicktrack = new { settings = new { enable = 0 } }
                    }
                });

                MailMessage mailMessage = new MailMessage()
                    { Subject = _options.Value.Subject };
                mailMessage.To.Add(new MailAddress(email));
                mailMessage.From = new MailAddress(_options.Value.Sender);
                var msgHtml = _options.Value.TemplateMailMessageContents(callbackUrl);
                var view = AlternateView.CreateAlternateViewFromString(msgHtml, null, MediaTypeNames.Text.Html);
                var plainView = AlternateView.CreateAlternateViewFromString(callbackUrl, null, MediaTypeNames.Text.Plain);
                mailMessage.AlternateViews.Add(view);
                mailMessage.AlternateViews.Add(plainView);
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = msgHtml;
                mailMessage.Headers.Add("X-SMTPAPI", options.ToString());
                await _smtp.SendMailAsync(mailMessage);

              
                await _options.Value.ResponseSuccessFullAsync(httpcontext);
             
               
                //TODO make this option provided

            };
        }

        public async Task<(ClaimsPrincipal, string,string)> OnCallback(HttpContext httpcontext)
        {
            var handleId = httpcontext.Request.Query["token"].FirstOrDefault();
            var (ticketInfo, redirectUri) = await _options.Value.GetTicketInfoAsync(httpcontext,handleId);

            var ticket = QueryHelpers.ParseNullableQuery
            (Encoding.UTF8.GetString(CryptographyHelpers.Decrypt(handleId.Sha512(), handleId.Sha1(),
                ticketInfo)));

            var identity = new ClaimsIdentity(
                ticket.Select(kv => new Claim(kv.Key, kv.Value)).ToArray(),
                AuthenticationName);

            return await Task.FromResult((new ClaimsPrincipal(identity), redirectUri, handleId));
        }

        public RequestDelegate OnSignout(string callbackUrl)
        {
            throw new NotImplementedException();
        }

        public RequestDelegate OnSignedOut()
        {
            throw new NotImplementedException();
        }

        public RequestDelegate OnSingleSignOut(string callbackUrl)
        {
            throw new NotImplementedException();
        }
    }
}

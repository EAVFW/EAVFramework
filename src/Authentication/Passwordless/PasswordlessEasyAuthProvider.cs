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
using EAVFramework.Extensions;
using Microsoft.Extensions.Logging;
using EAVFramework.OpenTelemetry;
using EAVFramework.Configuration;

namespace EAVFramework.Authentication.Passwordless
{

    public abstract class DefaultAuthProvider : IEasyAuthProvider
    {
        private readonly string _authSchema;
        private readonly HttpMethod _httpMethod;

        public DefaultAuthProvider(string authSchema, HttpMethod httpMethod = null)
        {
            _authSchema = authSchema;
            _httpMethod = httpMethod ?? HttpMethod.Get;
        }
        public string AuthenticationName => _authSchema;
        public virtual HttpMethod CallbackHttpMethod => _httpMethod;
        public virtual bool AutoGenerateRoutes { get; } = true;

        public abstract Task<OnAuthenticateResult> OnAuthenticate(OnAuthenticateRequest authenticateRequest);
        public abstract Task<OnCallBackResult> OnCallback(OnCallbackRequest request);
        public virtual RequestDelegate OnSignout(string callbackUrl)
        {
            throw new NotImplementedException();
        }

        public virtual RequestDelegate OnSignedOut()
        {
            throw new NotImplementedException();
        }

        public virtual RequestDelegate OnSingleSignOut(string callbackUrl)
        {
            throw new NotImplementedException();
        }

        public virtual Task PopulateCallbackRequest(OnCallbackRequest request)
        {
            request.HandleId = request.HttpContext.Request.Query.TryGetValue("token", out var token) ? Guid.Parse(token) : default;
            return Task.CompletedTask;
        }
    }

    public class PasswordlessEasyAuthProvider : DefaultAuthProvider
    {
        private readonly ILogger<PasswordlessEasyAuthProvider> _logger;
        private readonly EAVMetrics _metrics;
        private readonly SmtpClient _smtp;
        private readonly IOptions<PasswordlessEasyAuthOptions> _options;

        public PasswordlessEasyAuthProvider() :base("passwordless") { }

        public PasswordlessEasyAuthProvider(
            ILogger<PasswordlessEasyAuthProvider> logger,
            EAVMetrics metrics,
            SmtpClient smtpClient,
            IOptions<PasswordlessEasyAuthOptions> options) : this()
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics;
            _smtp = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

     
       

        public override async Task<OnAuthenticateResult> OnAuthenticate(OnAuthenticateRequest authenticateRequest)
        {
            // return async (httpcontext) =>
            {

                if (!authenticateRequest.IdentityId.HasValue || authenticateRequest.IdentityId.Value == default)
                {    
                      return new OnAuthenticateResult { ErrorMessage = $"User not found for email",
                          ErrorCode= "access_denied", ErrorSubCode = "user_not_found", Success=false };
                }

                
                var email = await authenticateRequest.Options.FindEmailFromIdentity(
                    new EmailDiscoveryRequest
                    {
                        HttpContext = authenticateRequest.HttpContext,
                        IdentityId = authenticateRequest.IdentityId.Value,
                        ServiceProvider = authenticateRequest.ServiceProvider
                    });


               

                //  var ticket = CryptographyHelpers.Encrypt(handleId.Sha512(), handleId.Sha1(),
                //      Encoding.UTF8.GetBytes($"sub={user}&email={email}"));

                //  await _options.Value.PersistTicketAsync(httpcontext, user, handleId,ticket, redirectUri);

                var options = JToken.FromObject(new
                {
                    unique_args = new Dictionary<string, string>
                    {
                        ["email_id"] = authenticateRequest.HandleId.ToString().URLSafeHash(),
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
                var msgHtml = _options.Value.TemplateMailMessageContents(authenticateRequest.CallbackUrl);
                var view = AlternateView.CreateAlternateViewFromString(msgHtml, null, MediaTypeNames.Text.Html);
                var plainView = AlternateView.CreateAlternateViewFromString(authenticateRequest.CallbackUrl, null, MediaTypeNames.Text.Plain);
                mailMessage.AlternateViews.Add(view);
                mailMessage.AlternateViews.Add(plainView);
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = msgHtml;
                mailMessage.Headers.Add("X-SMTPAPI", options.ToString());

                _logger.LogInformation("Sending sigin email to {email} with handleId {handleId} from {sender}",
                    MaskEmail(email), authenticateRequest.HandleId, _options.Value.Sender);

                await _smtp.SendMailAsync(mailMessage);


                await _options.Value.ResponseSuccessFullAsync(authenticateRequest.HttpContext);

                return new OnAuthenticateResult { Success = true };

                //TODO make this option provided

            };
        }
        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return email;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex < 0)
            {
                return email;
            }

            var localPart = email.Substring(0, atIndex);
            var domain = email.Substring(atIndex);

            if (localPart.Length <= 3)
            {
                return $"{localPart}***{domain}";
            }

            var maskedLocalPart = localPart.Substring(0, 3) + new string('*', localPart.Length - 3);
            return $"{maskedLocalPart}{domain}";
        }
        public override async Task<OnCallBackResult> OnCallback(OnCallbackRequest request)
        {


            var ticket = request.Ticket;

            var identity = new ClaimsIdentity(
                [new Claim("sub",request.IdentityId.ToString()), ..
                ticket.Select(kv => new Claim(kv.Key, kv.Value)).ToArray()],
                AuthenticationName);



            return new OnCallBackResult
            {
                Principal = new ClaimsPrincipal(identity),

                Success = true
            };

        }

       
    }
}

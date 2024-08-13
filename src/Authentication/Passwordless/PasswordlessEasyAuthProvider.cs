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
using Sprache;

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
    public class EAVEMailService
    {
        private readonly SmtpClient _smtp;
        private readonly ILogger<EAVEMailService> _logger;

        public EAVEMailService(SmtpClient smtpClient, ILogger<EAVEMailService> logger)
        {
            _smtp = smtpClient;
            _logger = logger;
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

        public async Task SendEmailAsync(Guid emailId, string subject, string sender, string to_emails, string msgHtml, string msgPlain, string sender_displayname=null)
        {
            var options = JToken.FromObject(new
            {
                unique_args = new Dictionary<string, string>
                {
                    ["email_id"] = emailId.ToString().URLSafeHash(),
                },
                filters = new
                {
                    opentrack = new { settings = new { enable = 0 } },
                    clicktrack = new { settings = new { enable = 0 } }
                }
            });

            MailMessage mailMessage = new MailMessage()
            {
                Subject = subject
            };
            foreach (var email in to_emails.Split(',', ';'))
            {
                mailMessage.To.Add(new MailAddress(email));
            }

            mailMessage.From = new MailAddress(sender,sender_displayname,Encoding.UTF8);
         
            var view = AlternateView.CreateAlternateViewFromString(msgHtml, null, MediaTypeNames.Text.Html);
            var plainView = AlternateView.CreateAlternateViewFromString(msgPlain, null, MediaTypeNames.Text.Plain);
            mailMessage.AlternateViews.Add(view);
            mailMessage.AlternateViews.Add(plainView);
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = msgHtml;
            mailMessage.Headers.Add("X-SMTPAPI", options.ToString());

            _logger.LogInformation("Sending email to {email} with emailid {emailid} from {sender}",
               string.Join(",", to_emails.Split(',', ';').Select(email=>  MaskEmail(email))),
               emailId, sender);



            await SendEmailWithRetryAsync(mailMessage);

        }

        private async Task SendEmailWithRetryAsync(MailMessage mailMessage)
        {
            const int maxRetries = 5;
            int retryCount = 0;
            int delay = 40; // Initial delay in milliseconds 

            while (retryCount < maxRetries)
            {
                try
                {
                    await _smtp.SendMailAsync(mailMessage);

                    return;

                }
                catch (Exception ex) when (ex.Message.Contains("Service not available, closing transmission channel"))
                {

                    _logger.LogWarning(ex, "Failed to send email. Attempt {retryCount} of {maxRetries}", retryCount + 1, maxRetries);

                    if (retryCount == maxRetries - 1)
                    {
                        throw;
                    }

                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff
                    retryCount++;
                }
            }


        }

    }
    public class PasswordlessEasyAuthProvider : DefaultAuthProvider
    {
        private readonly ILogger<PasswordlessEasyAuthProvider> _logger;
        private readonly EAVEMailService _emailService;
        private readonly EAVMetrics _metrics;
     //   private readonly SmtpClient _smtp;
        private readonly IOptions<PasswordlessEasyAuthOptions> _options;

        public PasswordlessEasyAuthProvider() :base("passwordless") { }

        public PasswordlessEasyAuthProvider(
            ILogger<PasswordlessEasyAuthProvider> logger,
            EAVEMailService emailService, // SmtpClient smtpClient,
            IOptions<PasswordlessEasyAuthOptions> options, EAVMetrics metrics=null) : this()
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService;
            _metrics = metrics;
         //   _smtp = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

     
       

        public override async Task<OnAuthenticateResult> OnAuthenticate(OnAuthenticateRequest authenticateRequest)
        {
            // return async (httpcontext) =>
            {
                var email = authenticateRequest.Email;

                if (!authenticateRequest.IdentityId.HasValue || authenticateRequest.IdentityId.Value == default)
                {    
                      return new OnAuthenticateResult { ErrorMessage = $"User not found",
                          ErrorCode= "access_denied", ErrorSubCode = "user_not_found", Success=false };
                }

                //  var ticket = CryptographyHelpers.Encrypt(handleId.Sha512(), handleId.Sha1(),
                //      Encoding.UTF8.GetBytes($"sub={user}&email={email}"));

                //  await _options.Value.PersistTicketAsync(httpcontext, user, handleId,ticket, redirectUri);


                await _emailService.SendEmailAsync(authenticateRequest.HandleId,
                    subject: _options.Value.Subject,
                    sender: _options.Value.Sender,
                    to_emails: email,
                    msgHtml: _options.Value.TemplateMailMessageContents(authenticateRequest.CallbackUrl),
                    msgPlain: authenticateRequest.CallbackUrl);

              
                await _options.Value.ResponseSuccessFullAsync(authenticateRequest.HttpContext);

                return new OnAuthenticateResult { Success = true };

                //TODO make this option provided

            };
        }

       

       
        public override async Task<OnCallBackResult> OnCallback(OnCallbackRequest request)
        {


            var ticket = request.Ticket;

            if(!request.IdentityId.HasValue || request.IdentityId.Value == default)
            {
                 request.IdentityId = await request.Options?.FindIdentity(
                          new UserDiscoveryRequest { 
                                        
                            Email = ticket["email"].ToString(),
                            HttpContext = request.HttpContext,
                            ServiceProvider = request.HttpContext.RequestServices
                    });
            }

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

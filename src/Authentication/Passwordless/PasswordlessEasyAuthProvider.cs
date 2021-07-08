using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication.Passwordless
{
    public class PasswordlessEasyAuthProvider : IEasyAuthProvider
    {
      //  private readonly CloudStorageAccount _storage;
        private readonly SmtpClient _smtp;
        private readonly IOptions<PasswordlessEasyAuthOptions> _options;
        
        public PasswordlessEasyAuthProvider() {}
        
        public PasswordlessEasyAuthProvider(
          //  CloudStorageAccount storage,
            SmtpClient smtpClient,
            IOptions<PasswordlessEasyAuthOptions> options)
        {
           // _storage = storage ?? throw new ArgumentNullException((nameof(storage)));
            _smtp = smtpClient ?? throw new ArgumentNullException(nameof(smtpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string AuthenticationName => "passwordless";

        public RequestDelegate OnAuthenticate(string handleId, string callbackUrl)
        {
            return async (httpcontext) =>
            {
                //var table = _storage.CreateCloudTableClient().GetTableReference("signin");
              //  await table.CreateIfNotExistsAsync();

                // var data = await JToken.ReadFromAsync(
                //     new JsonTextReader(new StreamReader(httpcontext.Request.Body)));
                // var email = data.SelectToken("$.email")?.ToString();
                var email = httpcontext.Request.Query["email"].FirstOrDefault();
                var redirectUri = httpcontext.Request.Query["redirectUri"].FirstOrDefault();

                var user = await _options.Value.FetchUserIdByEmailAsync(httpcontext,httpcontext.RequestServices, email);

                if (user == null)
                {
                    var uriBuilder = new UriBuilder(callbackUrl);
                  
                    httpcontext.Response.Redirect(callbackUrl  + $"{(callbackUrl.Contains('?')?"&":"?")}error=access_denied&error_subcode=user_not_found");
                  //  _options.Value.OnNotFound(httpcontext);
                    return;
                }

                var ticket = CryptographyHelpers.Encrypt(handleId.Sha512(), handleId.Sha1(),
                    Encoding.UTF8.GetBytes($"sub={user}&email={email}"));

                //await table.ExecuteAsync(TableOperation.InsertOrReplace(new DynamicTableEntity
                //{
                //    ETag = "*", PartitionKey = handleId.URLSafeHash(), RowKey = "", Properties = new
                //        Dictionary<string, EntityProperty>
                //        {
                //            ["ticket"] = EntityProperty.GeneratePropertyForByteArray(ticket),
                //            ["redirectUri"] =
                //                EntityProperty.GeneratePropertyForString(redirectUri)
                //        }
                //}));

                await _options.Value.PersistTicketAsync(httpcontext,handleId.URLSafeHash(),ticket, redirectUri);


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

                var webRootPath = httpcontext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                await httpcontext.Response.SendFileAsync($"{webRootPath}/account/login/passwordless/index.html");
               
                //TODO make this option provided

            };
        }

        public async Task<(ClaimsIdentity, string)> OnCallback(string handleId, HttpContext httpcontext)
        {
            //var table = _storage.CreateCloudTableClient().GetTableReference("signin");

            var (ticketInfo, redirectUri) = await _options.Value.GetTicketInfoAsync(httpcontext,handleId.URLSafeHash());

            //  var ticketInfo = table.CreateQuery<DynamicTableEntity>()
            //       .Where(c => c.PartitionKey == handleId.URLSafeHash()).Take(1).ToList().FirstOrDefault();
            var ticket = QueryHelpers.ParseNullableQuery
            (Encoding.UTF8.GetString(CryptographyHelpers.Decrypt(handleId.Sha512(), handleId.Sha1(),
                ticketInfo)));

            //var redirectUri = ticketInfo.Properties["redirectUri"].StringValue;
            return await Task.FromResult((new ClaimsIdentity(ticket.Select(kv => new Claim(kv.Key, kv.Value)).ToArray(),
                AuthenticationName), redirectUri));
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication.Passwordless
{
    public class PasswordlessEasyAuthOptions //: IEasyAuthOptions<PasswordlessEasyAuthProvider>
    {
        /// <summary>
        /// The subject line of the sign-in email
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// The email address the sign-in email should originate 'From'
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Function for taking an email address and looking up the unique ID for the corresponding user.
        /// If this function returns null, the OnNotFound delegate will be executed.
        /// </summary>
        public Func<HttpContext, IServiceProvider, string, Task<string>> FetchUserIdByEmailAsync { get; set; }

        /// <summary>
        /// Function for taking the generated magic link and interpolating it into the desired sign-in email response
        /// body.
        /// </summary>
        public Func<string, string> TemplateMailMessageContents { get; set; }

        /// <summary>
        /// Delegate for decorating the HttpContext in the scenario that a user cannot be looked up.
        /// You can use this to, among other things, set a custom response code or perform a redirect.
        /// </summary>
        public Action<HttpContext> OnNotFound { get; set; } = context => { context.Response.StatusCode = 404; };
    }
}

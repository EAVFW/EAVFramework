using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace EAVFramework.Authentication.Passwordless
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
        /// Function for taking the generated magic link and interpolating it into the desired sign-in email response
        /// body.
        /// </summary>
        public Func<string, string> TemplateMailMessageContents { get; set; }

        /// <summary>
        /// Delegate for decorating the HttpContext in the scenario that a user cannot be looked up.
        /// You can use this to, among other things, set a custom response code or perform a redirect.
        /// </summary>
        public Action<HttpContext> OnNotFound { get; set; } = context => { context.Response.StatusCode = 404; };

     
        public Func<HttpContext, Task> ResponseSuccessFullAsync { get; set; } = DefaultResponse;

        private static async Task DefaultResponse(HttpContext httpcontext)
        {
            var webRootPath = httpcontext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
            await httpcontext.Response.SendFileAsync($"{webRootPath}/account/login/passwordless/index.html");
        }

        public bool MaskEmailInLogs { get; set; } = true;
    }
}

using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using EAVFramework.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EAVFramework.Authentication
{
    public interface IEasyAuthProvider
    {
        public string AuthenticationName { get; }
        public HttpMethod CallbackHttpMethod { get; }
        public bool AutoGenerateRoutes { get; set; }
        Task OnAuthenticate(HttpContext httpcontext,string handleId, string redirectUrl);
        Task<(ClaimsPrincipal, string,string)> OnCallback(HttpContext httpcontext);
        public RequestDelegate OnSignout(string callbackUrl);
        public RequestDelegate OnSignedOut();
        public RequestDelegate OnSingleSignOut(string callbackUrl);
    }
 
}

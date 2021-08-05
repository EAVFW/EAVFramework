using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public interface IEasyAuthProvider
    {
        public string AuthenticationName { get; }
        public HttpMethod CallbackHttpMethod { get; }
        public bool AutoGenerateRoutes { get; set; }
        RequestDelegate OnAuthenticate(string handleId, string redirectUrl);
        Task<(ClaimsPrincipal, string)> OnCallback(string handleId, HttpContext httpcontext);
        public RequestDelegate OnSignout(string callbackUrl);
        public RequestDelegate OnSignedOut();
        public RequestDelegate OnSingleSignOut(string callbackUrl);
    }
    
    public class AuthProviderMetadata<T> where T : IEasyAuthProvider
    {
        private static readonly PropertyInfo prop = typeof(T).GetProperty("AuthenticationName");

        public string AuthenticationName()
        {
            return (string) prop.GetValue(null);
        }
    }
 
}

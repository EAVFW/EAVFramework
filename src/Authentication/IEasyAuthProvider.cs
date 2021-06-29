using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public interface IEasyAuthProvider
    {
        public string AuthenticationName { get; }
        RequestDelegate OnAuthenticate(string handleId, string redirectUrl);
        Task<(ClaimsIdentity, string)> OnCallback(string handleId, HttpContext httpcontext);
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

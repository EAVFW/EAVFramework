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
}

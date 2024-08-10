using EAVFramework.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EAVFramework.Authentication
{
    public class OnCallBackResult
    {
        public ClaimsPrincipal Principal { get; set; }
        
        

        public bool Success { get; set; }

        public string ErrorMessage { get; set; }
        public string ErrorCode { get;  set; }
        public string ErrorSubCode { get;  set; }
    }
    public class OnAuthenticateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorSubCode { get; set; }
    }
    public interface IEasyAuthProvider
    {
        public string AuthenticationName { get; }
        public HttpMethod CallbackHttpMethod { get; }
        public bool AutoGenerateRoutes { get; }
        Task<OnAuthenticateResult> OnAuthenticate(OnAuthenticateRequest authenticateRequest);
        Task<OnCallBackResult> OnCallback(OnCallbackRequest request);
        public RequestDelegate OnSignout(string callbackUrl);
        public RequestDelegate OnSignedOut();
        public RequestDelegate OnSingleSignOut(string callbackUrl);

        public Task PopulateCallbackRequest(OnCallbackRequest request);
    }
 
}

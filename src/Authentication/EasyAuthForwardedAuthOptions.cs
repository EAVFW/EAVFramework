using Microsoft.AspNetCore.Authentication;

namespace EAVFramework.Authentication
{
    public class EasyAuthForwardedAuthOptions : AuthenticationSchemeOptions
    {
        public string HeaderName { get; set; } = "X-EasyAuth-UserID";
        public string HeaderPrefix { get; set; } = "X-EasyAuth-";
    }
 
}

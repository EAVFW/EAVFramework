using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EAVFramework.Configuration
{
    public class MultiTenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<EAVFrameworkOptions> _options;

        public Dictionary<string, object> Props => Context.Value;

        private static System.Threading.AsyncLocal<string> Host { get; } = new System.Threading.AsyncLocal<string>();
        private static System.Threading.AsyncLocal<Dictionary<string, object>> Context { get; } = new System.Threading.AsyncLocal<Dictionary<string, object>>();
        public MultiTenantContext(IHttpContextAccessor httpContextAccessor, IOptions<EAVFrameworkOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
            Context.Value = new Dictionary<string, object>();
        }
        public void SetMembershipHost(string host)
        {
            Host.Value = host;
        }

        public string GetMembershipHost()
        { 
            return _httpContextAccessor?.HttpContext?.Request?.Host.Value ?? Host.Value;
        }

        public ClaimsPrincipal GetSystemPrincipal()
        {
            return _options.Value.SystemAdministratorIdentity;
        }
    }
}

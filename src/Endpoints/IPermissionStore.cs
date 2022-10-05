using System.Linq;
using System.Security.Claims;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public interface IPermissionStore 
    {
        IQueryable<string> GetPermissions(ClaimsPrincipal user, EAVResource resource);
    }
    public interface IPermissionStore<TContext> where TContext : DynamicContext
    {
        IQueryable<string> GetPermissions(ClaimsPrincipal user, EAVResource resource);
    }
    public class DefaultPermissionStore<TContext> : IPermissionStore<TContext> where TContext : DynamicContext
    {
        public IQueryable<string> GetPermissions(ClaimsPrincipal user, EAVResource resource)
        {
            return Enumerable.Empty<string>().AsQueryable();
        }
    }
}

using System.Linq;
using System.Security.Claims;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public interface IPermissionStore
    {
        IQueryable<string> GetPermissions(ClaimsPrincipal user, EAVResource resource);
    }
}

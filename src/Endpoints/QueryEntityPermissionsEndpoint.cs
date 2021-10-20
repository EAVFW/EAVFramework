using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class QueryEntityPermissionsEndpoint<TContext> : IEndpointHandler
     where TContext : DynamicContext
    {
        private readonly EAVDBContext<TContext> _context;
        private readonly IPermissionStore permissionStore;

        public QueryEntityPermissionsEndpoint(
             EAVDBContext<TContext> context,
            IPermissionStore permissionStore)
        {
            this._context = context;
            this.permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
            var resource = _context.CreateEAVResource(entityName);

            var permissions = await permissionStore.GetPermissions(context.User, resource).ToListAsync();

            return new DataEndpointResult(new { permissions = permissions });
        }
    }
}

using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class RetrieveRecordEndpoint<TContext> : IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly TContext _context;

        public RetrieveRecordEndpoint(TContext context)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
            context.Request.QueryString= context.Request.QueryString.Add("$filter", $"id eq {recordId}");

            var query = await _context.ExecuteHttpRequest(entityName, context.Request);

            if (!query.Items.Any())
                return new NotFoundResult();

            return new DataEndpointResult(new { value = query.Items.First() });


        }
    }
}

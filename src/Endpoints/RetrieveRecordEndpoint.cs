using EAVFramework.Endpoints.Results;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;
using static EAVFramework.Constants;

namespace EAVFramework.Endpoints
{
    public class RetrieveRecordEndpoint<TContext> : IEndpointHandler<TContext>
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

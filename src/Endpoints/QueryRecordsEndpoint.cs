using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    public class QueryRecordsEndpoint<TContext> : IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;

        public QueryRecordsEndpoint(TContext context)
        {
            _context = context;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var result = await _context.ExecuteHttpRequest(routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string, context.Request);

            return new DataEndpointResult(new { items = result.Items.ToArray() });


        }
    }
}

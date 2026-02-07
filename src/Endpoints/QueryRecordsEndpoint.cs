using EAVFramework.Configuration;
using EAVFramework.Endpoints.Results;
using EAVFramework.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EAVFramework.Constants;

namespace EAVFramework.Endpoints
{


    public class QueryRecordsEndpoint<TContext> : IEndpointHandler<TContext>
        where TContext : DynamicContext
    {
        private readonly TContext _context;
        private readonly IOptions<EAVFrameworkOptions> options;

        public QueryRecordsEndpoint(TContext context, IOptions<EAVFrameworkOptions> options)
        {
            _context = context;
            this.options = options;
        }
        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var result = await _context.ExecuteHttpRequest(routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string, context.Request);

            //https://github.com/OData/odata.net/blob/ae104c923e49069eb665f3963d1ca6d8ea72e69c/src/Microsoft.OData.Core/Json/ODataJsonCollectionSerializer.cs#L97
            //The pageresult is not true odata output
            //Featureflagging true odata with odata.context and odata.context

            if (this.options.Value.ODataOptions.UseODataContextCountSerialization)
            {
                var type = _context.Manager.ModelDefinition.EntityDTOs[routeValues[RouteParams.EntityCollectionSchemaNameRouteParam].ToString().Replace(" ", "")];
                return new ODataEndpointResult(type, result);
            }


            return new DataEndpointResult(result.ToDictionary());


        }
    }
}
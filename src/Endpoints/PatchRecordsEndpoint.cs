using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Validation;
using static DotNetDevOps.Extensions.EAVFramework.Constants;
using System.Security.Claims;
using System.Collections;
using System.Reflection;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{

    internal class PatchRecordsEndpoint<TContext> : IEndpointHandler
      where TContext : DynamicContext
    {
        private readonly EAVDBContext<TContext> _context;
        private readonly ILogger<PatchRecordsEndpoint<TContext>> logger;
        private readonly IConfiguration configuration;

        public PatchRecordsEndpoint(
            EAVDBContext<TContext> context,
            ILogger<PatchRecordsEndpoint<TContext>> logger,
            IConfiguration configuration

            )
        {
            _context = context;
            this.logger = logger;
            this.configuration = configuration;
        }


        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var recordId = routeValues[RouteParams.RecordIdRouteParam] as string;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;
             
            JToken record = await _context.ReadRecordAsync(context,new ReadOptions {RecordId= recordId, LogPayload = configuration.GetValue<bool>($"EAVFramework:PatchRecordsEndpoint:LogPayload", false) });

            var entity = await _context.PatchAsync(entityName, Guid.Parse(recordId), record);
             
            var _operation = await _context.SaveChangesAsync(context.User);


            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });

            return new DataEndpointResult(new { id = entity.CurrentValues.GetValue<Guid>("Id") });



        }

      
    }
}

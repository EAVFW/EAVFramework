using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DotNetDevOps.Extensions.EAVFramework.Constants;

namespace DotNetDevOps.Extensions.EAVFramework.Endpoints
{
    internal class BaseEndpoint
    {
        private readonly IEnumerable<EntityPlugin> _plugins;
        public BaseEndpoint(IEnumerable<EntityPlugin> plugins)
        {
            _plugins = plugins;
        }
        protected async Task RunPostOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type == a.Entity.GetType()))
            {
                await plugin.Execute(context.RequestServices, a);
            }

            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunPostOperation(context, navigation.TargetEntry);
            }
        }

        protected async Task RunPreOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunPreOperation(context, navigation.TargetEntry);
            }

            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreOperation && plugin.Type == a.Entity.GetType()))
            {
                await plugin.Execute(context.RequestServices, a);
            }
        }

        protected async Task<List<ValidationError>> RunPreValidation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {


            var errors = new List<ValidationError>();

            foreach(var navigation in a.References.Where(t=>t.TargetEntry !=null))
            {
                errors.AddRange(await RunPreValidation(context, navigation.TargetEntry));
            }

            foreach (var plugin in _plugins.Where(plugin => plugin.Execution == EntityPluginExecution.PreValidate && plugin.Type == a.Entity.GetType()))
            {
                var ctx = await plugin.Execute(context.RequestServices, a);
                errors.AddRange(ctx.Errors);
            }

            return errors;
        }

    }
    internal class CreateRecordsEndpoint<TContext> : BaseEndpoint, IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;
      
        private readonly ILogger<CreateRecordsEndpoint<TContext>> _logger;

        public CreateRecordsEndpoint(
            TContext context,
            IEnumerable<EntityPlugin> plugins,
            ILogger<CreateRecordsEndpoint<TContext>> logger) : base(plugins.Where(c=>c.Operation == EntityPluginOperation.Create))
        {
            _context = context;           
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;


            var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));



           

            var a = _context.Add(entityName, record);

            List<ValidationError> errors = await RunPreValidation(context, a);

            if (errors.Any())
                return new DataValidationErrorResult(new { errors = errors });

            using var transaction = _context.Database.BeginTransaction();

            await RunPreOperation(context, a);

            await _context.SaveChangesAsync();

            await RunPostOperation(context, a);

            transaction.Commit();


            return new DataEndpointResult(new { id = a.CurrentValues.GetValue<Guid>("Id") });





        }

     
    }
}

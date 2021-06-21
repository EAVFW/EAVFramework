using DotNetDevOps.Extensions.EAVFramework.Endpoints.Results;
using DotNetDevOps.Extensions.EAVFramework.Hosting;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        private readonly IPluginScheduler _pluginScheduler;

        public BaseEndpoint(IEnumerable<EntityPlugin> plugins, IPluginScheduler pluginScheduler)
        {
            _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
            _pluginScheduler = pluginScheduler ?? throw new ArgumentNullException(nameof(pluginScheduler));
        }

        protected async Task RunAsyncPostOperation(HttpContext context, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry a)
        {
            foreach (var plugin in _plugins.Where(plugin => plugin.Mode == EntityPluginMode.Async && plugin.Execution == EntityPluginExecution.PostOperation && plugin.Type == a.Entity.GetType()))
            {
                await _pluginScheduler.ScheduleAsync(plugin, a.Entity);
               // await plugin.Execute(context.RequestServices, a);
            }

            foreach (var navigation in a.References.Where(t => t.TargetEntry != null))
            {
                await RunAsyncPostOperation(context, navigation.TargetEntry);
            }
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

    public class OperationContext<TContext>
    {
        public TContext Context { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public EntityEntry Entity { get;  set; }
    }
    internal class CreateRecordsEndpoint<TContext> : BaseEndpoint, IEndpointHandler
        where TContext : DynamicContext
    {
        private readonly TContext _context;
      
        private readonly ILogger<CreateRecordsEndpoint<TContext>> _logger;

        public CreateRecordsEndpoint(
            TContext context,
            IEnumerable<EntityPlugin> plugins,
            IPluginScheduler pluginScheduler,
            ILogger<CreateRecordsEndpoint<TContext>> logger) : base(plugins.Where(c=>c.Operation == EntityPluginOperation.Create), pluginScheduler)
        {
            _context = context;           
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            var routeValues = context.GetRouteData().Values;
            var entityName = routeValues[RouteParams.EntityCollectionSchemaNameRouteParam] as string;


            var record = await JToken.ReadFromAsync(new JsonTextReader(new StreamReader(context.Request.BodyReader.AsStream())));
             
            var strategy = _context.Database.CreateExecutionStrategy();


            var _operation = await strategy.ExecuteAsync(async () =>
            {
                var operation = new OperationContext<TContext>
                {
                    Context = _context
                };

                operation.Entity= _context.Add(entityName, record);

                operation.Errors = await RunPreValidation(context, operation.Entity);
                
                return operation;
            });
           


            if (_operation.Errors.Any())
                return new DataValidationErrorResult(new { errors = _operation.Errors });
             

            // using var transaction = _context.Database.BeginTransaction();

            await strategy.ExecuteInTransactionAsync(_operation,
                  operation: async (operation,ct) =>
                  {
                      await RunPreOperation(context, operation.Entity);

                      await operation.Context.SaveChangesAsync(acceptAllChangesOnSuccess: false);

                      await RunPostOperation(context, operation.Entity);
                  },

                  verifySucceeded: (operation,ct) => Task.FromResult(operation.Entity.CurrentValues.TryGetValue<Guid>("Id", out var id) && id != Guid.Empty)
                );

            _context.ChangeTracker.AcceptAllChanges();
             

            await RunAsyncPostOperation(context, _operation.Entity);

            return new DataEndpointResult(new { id = _operation.Entity.CurrentValues.GetValue<Guid>("Id") });





        }

     
    }
}

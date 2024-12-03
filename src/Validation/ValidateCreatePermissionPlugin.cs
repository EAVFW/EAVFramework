using System;
using System.Linq;
using System.Threading.Tasks;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using Microsoft.AspNetCore.Authorization;

namespace EAVFramework.Validation
{
    [PluginRegistration(EntityPluginExecution.PreValidate, EntityPluginOperation.Create, 0, EntityPluginMode.Sync)]
    public class ValidateCreatePermissionPlugin<TContext, TEntity> : IPlugin<TContext, TEntity> 
        where TContext : DynamicContext
         where TEntity : DynamicEntity
    {
         
        private readonly IAuthorizationService _authorizationService;

        public ValidateCreatePermissionPlugin(IPermissionStore<TContext> permissionStore, IAuthorizationService authorizationService)
        {
        
            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<TContext, TEntity> context)
        {
        

            var auth = await _authorizationService.AuthorizeAsync(context.User, context.EntityResource, new CreateRecordRequirement(context.EntityResource.EntityCollectionSchemaName, typeof(TContext)));

            if (!auth.Succeeded)
            {
                foreach (var err in auth.Failure.FailedRequirements.OfType<IAuthorizationRequirementError>()) {
                    context.AddError(err.ToError());
                        
                        };
              
            }
        }
    }
    [PluginRegistration(EntityPluginExecution.PreValidate, EntityPluginOperation.Update,0, EntityPluginMode.Sync)]
    public class ValidateUpdatePermissionPlugin<TContext,TEntity> : IPlugin<TContext, TEntity>
        where TContext:DynamicContext
        where TEntity : DynamicEntity
    {

        private readonly IAuthorizationService _authorizationService;

        public ValidateUpdatePermissionPlugin(IPermissionStore<TContext> permissionStore, IAuthorizationService authorizationService)
        {

            this._authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }
        public async Task Execute(PluginContext<TContext, TEntity> context)
        {


            var auth = await _authorizationService.AuthorizeAsync(context.User, context.EntityResource, new UpdateRecordRequirement(context.EntityResource.EntityCollectionSchemaName,typeof(TContext)));

            if (!auth.Succeeded)
            {
                foreach (var err in auth.Failure.FailedRequirements.OfType<IAuthorizationRequirementError>())
                {
                    context.AddError(err.ToError());

                };

            }
        }
    }
}
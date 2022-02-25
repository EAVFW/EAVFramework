using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using DotNetDevOps.Extensions.EAVFramework.Shared;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class ValidationPlugin<TDynamicContext> : IPlugin<TDynamicContext, DynamicEntity> where TDynamicContext : DynamicContext
    {
        private readonly IRetrieveMetaData<TDynamicContext> _metaData;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ValidatorMetaData> _validators;

        public ValidationPlugin(IRetrieveMetaData<TDynamicContext> metaData,
            IServiceProvider serviceProvider,
            IEnumerable<ValidatorMetaData> validators)
        {
            _metaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        }
        
        private static readonly ConcurrentDictionary<Type, string> _logicalNameMapping = new ConcurrentDictionary<Type, string>();
         
        public async Task Execute(PluginContext<TDynamicContext, DynamicEntity> context)
        {
            var metaData = await _metaData.GetAttributeMetaData( _logicalNameMapping.GetOrAdd( context.Input.GetType(), GetLogicalName));
    
            var form = context.Input;

            foreach(var property in form.GetType().GetProperties())
            {
                var attributeLogicalName = property.GetCustomAttribute<DataMemberAttribute>()?.Name;
                if (attributeLogicalName == null)
                    continue; // No dataMember attribute

                var attributeMetadata = metaData.FirstOrDefault(attr => attr.Value.SelectToken("$.logicalName")?.ToString() == attributeLogicalName);
                if (attributeMetadata == null)
                    continue;

                var value = property.GetValue(form);
                
                foreach (var validatorMetaData in _validators.Where(x => x.Type == property.PropertyType))
                {
                    if (validatorMetaData.ValidationPassed(_serviceProvider, value, attributeMetadata, out var error))
                        continue;
                    
                    error.AttributeSchemaName = attributeLogicalName;
                    error.EntityCollectionSchemaName = context.EntityResource.EntityCollectionSchemaName;
                    context.AddValidationError(error);
                }
            }
            
          
        }

        private string GetLogicalName(Type arg)
        {
            return arg.GetCustomAttribute<EntityDTOAttribute>().LogicalName;
        }
    }
}
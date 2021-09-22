using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using DotNetDevOps.Extensions.EAVFramework.Shared;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class ValidationPlugin : IPlugin<DynamicContext, DynamicEntity>
    {
        private readonly IRetrieveMetaData _metaData;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ValidatorMetaData> _validators;


        public ValidationPlugin(IRetrieveMetaData metaData,
            IServiceProvider serviceProvider,
            IEnumerable<ValidatorMetaData> validators)
        {
            _metaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        }
        private static ConcurrentDictionary<Type, string> _logicalNameMapping = new ConcurrentDictionary<Type, string>();
         
        public Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {

            var metaData = _metaData.GetAttributeMetaData( _logicalNameMapping.GetOrAdd( context.Input.GetType(), GetLogicalName));
    
            var form = context.Input;

            foreach(var property in form.GetType().GetProperties())
            {
                var attributeLogicalname = property.GetCustomAttribute<DataMemberAttribute>()?.Name;
                if (attributeLogicalname == null)
                    continue; // No dataMember attribute

              
              
                var attributeMetadata = metaData.FirstOrDefault(attr => attr.Value.SelectToken("$.logicalName")?.ToString() == attributeLogicalname);
                if (attributeMetadata == null)
                    continue;

                var value = property.GetValue(form);


                foreach (var validatorMetaData in _validators.Where(x => x.Type == property.PropertyType))
                {
                    if (!validatorMetaData.ValidationPassed(_serviceProvider, value, attributeMetadata, out var errorMessage))
                    {
                        context.AddValidationError(x => x, errorMessage, attributeLogicalname);
                    }
                }

            }
 
            
            return Task.CompletedTask;
        }

        private string GetLogicalName(Type arg)
        {
            return arg.GetCustomAttribute<EntityDTOAttribute>().LogicalName;
        }
    }
}
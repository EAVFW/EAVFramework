using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
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

        public Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {
            var metaData = _metaData.GetAttributeMetaData(context.Input.GetType().Name);
    
            var form = context.Input;

            var t = form.GetType().GetProperties() // 
                .Select(attr =>
                    (attrName: attr.Name, attrValue: attr.GetValue(form),
                        metaData: metaData.FirstOrDefault(x => ((JProperty) x).Name.Replace(" ", "") == attr.Name)
                            ?.First().SelectToken("$.type"))
                ).Where(attr => attr.attrValue != null && attr.metaData?.Type == JTokenType.Object);

            foreach (var (n, o, m) in t)
            {
                foreach (var validatorMetaData in _validators.Where(x => x.Type == o.GetType()))
                {
                    if (!validatorMetaData.ValidationPassed(_serviceProvider, o, m, out var errorMessage))
                    {
                        context.AddValidationError(x => x, errorMessage, n.ToLower());
                    }
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
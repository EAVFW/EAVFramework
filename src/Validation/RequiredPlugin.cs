using System;
using System.Linq;
using System.Threading.Tasks;
using EAVFramework.Endpoints;
using EAVFramework.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace EAVFramework.Validation
{
    public class RequiredPlugin : IPlugin<DynamicContext, DynamicEntity>
    {
        private readonly IRetrieveMetaData<DynamicContext> _metaData;
        private readonly RequiredSettings _requiredSettings;


        public RequiredPlugin(IRetrieveMetaData<DynamicContext> metaData, IOptions<RequiredSettings> requiredSettings)
        {
            _metaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            _requiredSettings = requiredSettings?.Value;
        }

        public async Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {
            var metaData = await _metaData.GetAttributeMetaData(context.Input.GetType().Name.ToLower());

            var form = context.Input;

            var requiredFields = metaData
                .Where(x => x.First().SelectToken("$.type.required")?.Value<bool>() ?? false)
                .Select(x => x.First().SelectToken("$.logicalName"));

            var nullAndRequiredFields = form.GetType().GetProperties()
                .Where(attr =>
                    (!_requiredSettings?.IgnoredFields?.Contains(attr.Name.ToLower()) ?? true)
                    && requiredFields.Contains(attr.Name.ToLower())
                    && attr.GetValue(form) == null)
                .Select(attr => attr.Name.ToLower());

            foreach (var nullField in nullAndRequiredFields)
            {
                context.AddValidationError( new ValidationError
                {
                    Error = "Is a required field",
                    Code = "err-required",
                    AttributeSchemaName = nullField,
                    EntityCollectionSchemaName = context.EntityResource.EntityCollectionSchemaName,
                });
            }

           
        }
    }
}
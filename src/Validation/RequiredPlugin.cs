using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetDevOps.Extensions.EAVFramework.Endpoints;
using DotNetDevOps.Extensions.EAVFramework.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.EAVFramework.Validation
{
    public class RequiredPlugin : IPlugin<DynamicContext, DynamicEntity>
    {
        private readonly IRetrieveMetaData _metaData;
        private readonly RequiredSettings _requiredSettings;


        public RequiredPlugin(IRetrieveMetaData metaData, IOptions<RequiredSettings> requiredSettings)
        {
            _metaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
            _requiredSettings = requiredSettings?.Value;
        }

        public Task Execute(PluginContext<DynamicContext, DynamicEntity> context)
        {
            var metaData = _metaData.GetAttributeMetaData(context.Input.GetType().Name.ToLower());

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

            return Task.CompletedTask;
        }
    }
}
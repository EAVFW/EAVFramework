using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class EntityDefinition
    {
        [JsonPropertyName("pluralName")] public string PluralName { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, AttributeDefinitionBase> Attributes { get; set; }
        [JsonPropertyName("wizards")] public Dictionary<string, WizardDefinition> Wizards { get; set; }
    }
}

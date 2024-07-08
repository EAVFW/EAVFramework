using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class EntityDefinition
    {
        [JsonProperty("pluralName")]
        [JsonPropertyName("pluralName")] 
        public string PluralName { get; set; }

        [JsonProperty("collectionSchemaName")]
        [JsonPropertyName("collectionSchemaName")]
        public string CollectionSchemaName { get; set; }
       
        [JsonProperty("logicalName")]
        [JsonPropertyName("logicalName")]
        public string LogicalName { get; set; }


        [JsonPropertyName("description")]
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("attributes")]
        [JsonProperty("attributes")]
        public Dictionary<string, AttributeDefinitionBase> Attributes { get; set; }
        
        [JsonPropertyName("wizards")]
        [JsonProperty("wizards")]
        public Dictionary<string, WizardDefinition> Wizards { get; set; }

        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [System.Text.Json.Serialization.JsonExtensionData]
        [Newtonsoft.Json.JsonExtensionData]
        public Dictionary<string, object> AdditionalFields { get; set; }
    }
}

using EAVFW.Extensions.Manifest.SDK.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class EntityDefinition
    {
        [JsonProperty("TPT")]
        [JsonPropertyName("TPT")]
        public string TPT { get; set; }
        [JsonProperty("TPC")]
        [JsonPropertyName("TPC")]
        public string TPC { get; set; }

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


        /// <summary>
        /// The database schema for this entity (table) created in the database.
        /// 
        /// When not defined a default schema is used from dynamic entity options, and if not defined there, the default schema dbo is used.
        /// </summary>
        [JsonPropertyName("schema")]
        [JsonProperty("schema")]
        public string Schema { get; set; }


        /// <summary>
        /// The database schema name for this entity. Typical the camelcase name of the entity
        /// </summary>
        [JsonPropertyName("schemaName")]
        [JsonProperty("schemaName")]

        public string SchemaName { get; set; }


        [JsonPropertyName("abstract")]
        [JsonProperty("abstract")]  
        public bool? Abstract { get; set; }

        [JsonPropertyName("mappingStrategy")]
        [JsonProperty("mappingStrategy")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public MappingStrategy? MappingStrategy { get; set; } = DTO.MappingStrategy.TPT;


        [JsonProperty("external")]
        [JsonPropertyName("external")]
        public bool? External { get; set; }
    }
}

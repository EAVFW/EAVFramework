using EAVFW.Extensions.Manifest.SDK.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    [System.Text.Json.Serialization.JsonConverter(typeof(KeyDefinitionSystemTextJsonConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(KeyDefinitionNewtonsoftConverter))]
    public class KeyDefinition
    {
        [JsonPropertyName("columns")]
        [JsonProperty("columns")]
        public string[] Columns { get; set; }

        [JsonPropertyName("unique")]
        [JsonProperty("unique")]
        public bool Unique { get; set; } = true;
    }

    /// <summary>
    /// System.Text.Json converter: handles both array format ["Col1"] and object format { "columns": ["Col1"], "unique": false }
    /// </summary>
    public class KeyDefinitionSystemTextJsonConverter : System.Text.Json.Serialization.JsonConverter<KeyDefinition>
    {
        public override KeyDefinition Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.StartArray)
            {
                var columns = System.Text.Json.JsonSerializer.Deserialize<string[]>(ref reader, options);
                return new KeyDefinition { Columns = columns, Unique = true };
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
            {
                string[] columns = null;
                bool unique = true;
                while (reader.Read())
                {
                    if (reader.TokenType == System.Text.Json.JsonTokenType.EndObject)
                        break;
                    if (reader.TokenType == System.Text.Json.JsonTokenType.PropertyName)
                    {
                        var prop = reader.GetString();
                        reader.Read();
                        if (string.Equals(prop, "columns", StringComparison.OrdinalIgnoreCase))
                            columns = System.Text.Json.JsonSerializer.Deserialize<string[]>(ref reader, options);
                        else if (string.Equals(prop, "unique", StringComparison.OrdinalIgnoreCase))
                            unique = reader.GetBoolean();
                    }
                }
                return new KeyDefinition { Columns = columns, Unique = unique };
            }
            return new KeyDefinition();
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, KeyDefinition value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("columns");
            System.Text.Json.JsonSerializer.Serialize(writer, value.Columns, options);
            writer.WriteBoolean("unique", value.Unique);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Newtonsoft converter: handles both array format ["Col1"] and object format { "columns": ["Col1"], "unique": false }
    /// </summary>
    public class KeyDefinitionNewtonsoftConverter : Newtonsoft.Json.JsonConverter<KeyDefinition>
    {
        public override KeyDefinition ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, KeyDefinition existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token is JArray array)
            {
                return new KeyDefinition { Columns = array.ToObject<string[]>(), Unique = true };
            }
            else if (token is JObject obj)
            {
                return new KeyDefinition
                {
                    Columns = obj["columns"]?.ToObject<string[]>(),
                    Unique = obj["unique"]?.ToObject<bool>() ?? true
                };
            }
            return new KeyDefinition();
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, KeyDefinition value, Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

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

        [JsonPropertyName("keys")]
        [JsonProperty("keys")]
        public Dictionary<string, KeyDefinition> Keys { get; set; }


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

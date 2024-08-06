using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class AttributeConverter : JsonConverter<AttributeDefinitionBase>
    {
        public override AttributeDefinitionBase Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return new AttributeStringDefinition { Value = reader.GetString() ?? "" };
                case JsonTokenType.StartObject:
                    var node = JsonNode.Parse(ref reader);
                    
                        var t = node.Deserialize<AttributeObjectDefinition>(new JsonSerializerOptions { Converters={ new TypeDefinitionConverter()} });
                        return t;
                    
                    
                default:
                    throw new Exception($"{reader.TokenType} is not supported as AttributeDefinition");
            }
        }

        public override void Write(Utf8JsonWriter writer, AttributeDefinitionBase value, JsonSerializerOptions options)
        {
            var jsonString = value switch
            {
                AttributeObjectDefinition attributeObjectDefinition =>
                    JsonSerializer.Serialize(attributeObjectDefinition),
                AttributeStringDefinition attributeStringDefinition =>
                    JsonSerializer.Serialize(attributeStringDefinition),
                _ => throw new ArgumentException($"{value.GetType()} is not supported")
            };

            var jsonNode = JsonNode.Parse(jsonString);
            jsonNode?.WriteTo(writer, options);
        }
    }

    [JsonConverter(typeof(AttributeConverter))]
    public abstract class AttributeDefinitionBase
    {
         
    }

    public class AttributeStringDefinition : AttributeDefinitionBase
    {
        public string Value { get; set; }
    }
    

    public class AttributeObjectDefinition : AttributeDefinitionBase
    {
        

        [JsonPropertyName("isPrimaryField")] public bool IsPrimaryField { get; set; }
        [JsonPropertyName("logicalName")] public string LogicalName { get; set; }
        [JsonPropertyName("schemaName")] public string SchemaName { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("moduleSource")] public string ModuleSource { get; set; }

        [JsonPropertyName("moduleLocation")] public string ModuleLocation { get; set; }

        [JsonPropertyName("type")] public TypeDefinition AttributeType { get; set; }

        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalFields { get; set; }

        
        
        [JsonPropertyName("isPrimaryKey")]
        public bool? IsPrimaryKey { get; set; }

        [JsonPropertyName("isRequired")]
        
        public bool? IsRequired { get; set; }
       
    

        [JsonPropertyName("isRowVersion")]
        public bool IsRowVersion { get; set; }
    }


    public class TypeDefinitionConverter : JsonConverter<TypeDefinition>
    {
        public override TypeDefinition Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return new TypeDefinition { Type = reader.GetString() ?? "" };
                case JsonTokenType.StartObject:
                    var node = JsonNode.Parse(ref reader);
                      
                        var t = node.Deserialize<TypeDefinition>();
                        return t;
                    
                   
                default:
                    throw new Exception($"{reader.TokenType} is not supported as AttributeDefinition");
            }
        }

        public override void Write(Utf8JsonWriter writer, TypeDefinition value, JsonSerializerOptions options)
        {
            var jsonString = value switch
            {
                TypeDefinition attributeObjectDefinition =>
                    JsonSerializer.Serialize(attributeObjectDefinition),
                
                _ => throw new ArgumentException($"{value.GetType()} is not supported")
            };

            var jsonNode = JsonNode.Parse(jsonString);
            jsonNode?.WriteTo(writer, options);
        }
    }

    // [JsonConverter(typeof(TypeDefinitionConverter))]
     
    public class CascadeOptions
    {
        [JsonPropertyName("delete")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CascadeAction? OnDelete { get; set; }

        [JsonPropertyName("update")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CascadeAction? OnUpdate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is CascadeOptions cascadeOptions)
            {
                return OnDelete == cascadeOptions.OnDelete && OnUpdate == cascadeOptions.OnUpdate;
            }
            
            return base.Equals(obj);
        }
    }

    public enum CascadeAction
    {
        /// <summary>
        ///     Do nothing. That is, just ignore the constraint.
        /// </summary>
        NoAction,

        /// <summary>
        ///     Don't perform the action if it would result in a constraint violation and instead generate an error.
        /// </summary>
        Restrict,

        /// <summary>
        ///     Cascade the action to the constrained rows.
        /// </summary>
        Cascade,

        /// <summary>
        ///     Set null on the constrained rows so that the constraint is not violated after the action completes.
        /// </summary>
        SetNull,

        /// <summary>
        ///     Set a default value on the constrained rows so that the constraint is not violated after the action completes.
        /// </summary>
        SetDefault
    }
    public class IndexInfo
    {
        [JsonPropertyName("unique")]
        public bool Unique { get; set; } = true;
      
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class TypeDefinition
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("split")] public bool Split { get; set; }
        [JsonPropertyName("referenceType")] public string ReferenceType { get; set; }

        [JsonPropertyName("options")] public Dictionary<string, JsonElement> Options { get; set; }

        [JsonPropertyName("sql")] public Dictionary<string, JsonElement> SqlOptions { get; set; }

        [JsonPropertyName("cascade")]
        public CascadeOptions Cascades {get;set;}

        [JsonPropertyName("index")]
        public IndexInfo IndexInfo { get; set; }

        [JsonPropertyName("required")]

        public bool? Required { get; set; }
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalFields { get; set; }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class StringOrBooleanTabConverter : JsonConverter<StringOrBoolean>
    {
        public override StringOrBoolean Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new StringOrBoolean(reader.GetString() ?? ""),
                JsonTokenType.False => new StringOrBoolean(false),
                JsonTokenType.True => new StringOrBoolean(true),
                _ => new StringOrBoolean()
            };
        }

        public override void Write(Utf8JsonWriter writer, StringOrBoolean value, JsonSerializerOptions options)
        {
            if (value.IsBool)
                writer.WriteBooleanValue(value.BooleanValue);
            writer.WriteStringValue(value.StringValue);
        }
    }
}

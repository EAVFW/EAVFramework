using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class MessageDefinition
    {
        [JsonPropertyName("title")] public string Title { get; set; }
    }
}

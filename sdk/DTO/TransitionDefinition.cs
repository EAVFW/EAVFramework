using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class TransitionDefinition
    {
        [JsonPropertyName("workflow")] public string Workflow { get; set; }

        [JsonPropertyName("message")] public MessageDefinition Message { get; set; }

        [JsonExtensionData] public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
}

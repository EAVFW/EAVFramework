using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public struct TriggerDefinition
    {
        [JsonPropertyName("form")] public string Form { get; set; }

        [JsonPropertyName("ribbon")] public string Ribbon { get; set; }
    }
}

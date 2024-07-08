using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class ManifestDefinition
    {
        [JsonPropertyName("entities")] public Dictionary<string, EntityDefinition> Entities { get; set; }
    }
}

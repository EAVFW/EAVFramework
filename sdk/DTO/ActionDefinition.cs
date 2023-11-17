using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class ActionDefinition
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("workflow")] public string Workflow { get; set; }
        
        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalFields { get; set; }
    }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public struct TriggerDefinition
    {
        [JsonPropertyName("form")] public string Form { get; set; }

     //   [JsonPropertyName("ribbon")] public string Ribbon { get; set; }

        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [System.Text.Json.Serialization.JsonExtensionData]
        [Newtonsoft.Json.JsonExtensionData]
        public Dictionary<string, object> AdditionalFields { get; set; }
    }
}

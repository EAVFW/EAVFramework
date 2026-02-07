using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class WizardDefinition
    {
        [JsonPropertyName("title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonPropertyName("triggers")]
        [JsonProperty("triggers")]
        public Dictionary<string, TriggerDefinition> Triggers { get; set; }

        [JsonPropertyName("tabs")]
        [JsonProperty("tabs")]
        public Dictionary<string, TabDefinition> Tabs { get; set; }

        /// <summary>
        /// Exclusively used to capture non-spec items
        /// </summary>
        [System.Text.Json.Serialization.JsonExtensionData]
        [Newtonsoft.Json.JsonExtensionData]
        public Dictionary<string, object> AdditionalFields { get; set; }
    }
}

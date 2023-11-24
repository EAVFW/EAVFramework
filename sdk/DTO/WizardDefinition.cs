using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class WizardDefinition
    {
        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("triggers")] public Dictionary<string, TriggerDefinition> Triggers { get; set; }

        [JsonPropertyName("tabs")] public Dictionary<string, TabDefinition> Tabs { get; set; }
    }
}

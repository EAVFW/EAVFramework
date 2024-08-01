using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class TabDefinition
    {
        [JsonPropertyName("columns")] public JsonElement Columns { get; set; }

        [JsonConverter(typeof(StringOrBooleanTabConverter))]
        [JsonPropertyName("visible")]
        public StringOrBoolean Visible { get; set; }

        [JsonPropertyName("onTransitionOut")] public TransitionDefinition OnTransitionOut { get; set; }

        [JsonPropertyName("onTransitionIn")] public TransitionDefinition OnTransitionIn { get; set; }

        [JsonPropertyName("actions")] public Dictionary<string, ActionDefinition> Actions { get; set; }
    }
}

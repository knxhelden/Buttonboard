using BSolutions.Buttonboard.Services.Enumerations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public sealed class SceneDefinition
    {

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed Scene";

        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("steps")]
        public List<SceneStep> Steps { get; set; } = new();

        [JsonIgnore]
        public ScenarioAssetKind Kind { get; set; } = ScenarioAssetKind.Scene;
    }
}

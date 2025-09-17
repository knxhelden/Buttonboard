using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public sealed class SceneDefinition
    {

        public SceneDefinition() { }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed Scene";

        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("steps")]
        public List<SceneStep> Steps { get; set; } = new();
    }
}

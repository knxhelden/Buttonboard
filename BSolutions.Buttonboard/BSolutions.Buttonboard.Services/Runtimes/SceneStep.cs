using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public sealed class SceneStep
    {
        public SceneStep() { }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Absolute timestamp in milliseconds for step start since scene start.
        /// </summary>
        [JsonPropertyName("startAtMs")]
        public int StartAtMs { get; set; }

        /// <summary>
        /// Aktion, z. B. "audio.play", "video.next", "video.pause", "gpio.on", "gpio.off", "mqtt.pub"
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Arguments as key/value (strings). Specific keys depend on the action.
        /// </summary>
        [JsonPropertyName("args")]
        public Dictionary<string, string>? Args { get; set; }

        /// <summary>
        /// Behavior in case of error
        /// </summary>
        /// <example>"continue" | "abort"</example>
        [JsonPropertyName("onError")]
        public string OnError { get; set; } = "continue";
    }
}

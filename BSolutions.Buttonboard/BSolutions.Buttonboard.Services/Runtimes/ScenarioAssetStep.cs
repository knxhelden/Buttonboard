using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Represents a single step in a <see cref="ScenarioAssetDefinition"/>.
    /// <para>
    /// Each step describes one action to be executed at a given timestamp
    /// relative to the start of the asset.
    /// </para>
    /// </summary>
    public sealed class ScenarioAssetStep
    {
        /// <summary>
        /// Optional display name for this step (e.g. for logging or authoring).
        /// Has no functional impact on execution.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Absolute timestamp in milliseconds from the start of the asset
        /// when this step should be executed.
        /// </summary>
        [JsonPropertyName("atMs")]
        public int AtMs { get; set; }

        /// <summary>
        /// Action identifier to execute.
        /// <para>
        /// Supported examples include:
        /// <c>gpio.on</c>, <c>gpio.off</c>, <c>audio.play</c>, <c>video.next</c>, <c>mqtt.pub</c>.
        /// </para>
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; }

        /// <summary>
        /// Optional arguments for the action, expressed as a dictionary.
        /// The set of keys/values depends on the specific action.
        /// Values are represented as <see cref="JsonElement"/> to allow arbitrary JSON literals.
        /// </summary>
        [JsonPropertyName("args")]
        public Dictionary<string, JsonElement>? Args { get; set; }

        /// <summary>
        /// Defines how execution should behave if this step fails.
        /// <list type="bullet">
        ///   <item><c>"continue"</c> → log error, continue with next step (default).</item>
        ///   <item><c>"abort"</c> → stop execution of the current asset immediately.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Defaults to <c>"continue"</c> if not specified.
        /// </remarks>
        [JsonPropertyName("onError")]
        public string OnError { get; set; } = "continue";
    }
}

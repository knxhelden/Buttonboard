using BSolutions.Buttonboard.Services.Enumerations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Represents a <b>scenario asset</b> loaded from a JSON file.
    /// <para>
    /// Scenario assets are either:
    /// <list type="bullet">
    ///   <item><b>Scenes</b> – triggered by buttons in a defined progression</item>
    ///   <item><b>Setup</b> – executed once during scenario initialization/reset</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class ScenarioAssetDefinition
    {
        /// <summary>
        /// Logical display name of the asset (optional, free text).
        /// Not used as lookup key – the file name defines the key.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unnamed asset";

        /// <summary>
        /// Version number for the asset definition. 
        /// Can be used to track iterations or ensure compatibility.
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Ordered list of steps that make up this asset.
        /// Steps are executed in sequence according to their <c>AtMs</c> offset.
        /// </summary>
        [JsonPropertyName("steps")]
        public List<ScenarioAssetStep> Steps { get; set; } = new();

        /// <summary>
        /// Kind of asset, derived from the file name during load
        /// (e.g. "setup.json" → <see cref="ScenarioAssetKind.Setup"/>).
        /// This property is ignored during serialization.
        /// </summary>
        [JsonIgnore]
        public ScenarioAssetKind Kind { get; set; } = ScenarioAssetKind.Scene;
    }
}
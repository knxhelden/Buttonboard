// Services/Settings/Options.cs
using BSolutions.Buttonboard.Services.Gpio;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BSolutions.Buttonboard.Services.Settings
{
    public sealed class ButtonboardOptions
    {
        [Required] public required ApplicationOptions Application { get; init; }
        [Required] public required ScenarioOptions Scenario { get; init; }
        [Required] public required OpenHabOptions OpenHAB { get; init; }
        [Required] public required VlcOptions VLC { get; init; }
        [Required] public required MqttOptions Mqtt { get; init; }
    }

    public sealed class ApplicationOptions
    {
        public bool DisableSceneOrder { get; init; }
        public OperationMode OperationMode { get; init; } = OperationMode.Real;

        [Required, MinLength(1)]
        public required string ScenarioAssetsFolder { get; init; }
    }

    public sealed class ScenarioOptions
    {
        [Required] public required SetupOptions Setup { get; init; }
        [Required, MinLength(1)] public required List<SceneMap> Scenes { get; init; }
    }

    public sealed class SetupOptions
    {
        // Default "setup" ist ok; Required stellt sicher, dass Key gesetzt/bindbar ist
        [Required, MinLength(1)]
        public string Key { get; init; } = "setup";
    }

    public sealed class SceneMap
    {
        [Required, MinLength(1)]
        public string Key { get; init; } = "";

        [Required]
        public Button TriggerButton { get; init; }

        [Range(0, int.MaxValue)]
        public int RequiredStage { get; init; }
    }

    public sealed class OpenHabOptions
    {
        [Required] public required Uri BaseUri { get; init; }

        /// <summary>
        /// Key = Player name, e.g. "Player1", "Player2"
        /// </summary>
        [Required, MinLength(1)]
        public required Dictionary<string, AudioPlayerOptions> Audio { get; init; }
    }

    public sealed class AudioPlayerOptions
    {
        [Range(0, 100)] public int Volume { get; init; } = 0;

        [Required] public required string ControlItem { get; init; }
        [Required] public required string StreamItem { get; init; }
        [Required] public required string VolumeItem { get; init; }
    }

    /// <summary>
    /// Dein JSON listet Player direkt unter "VLC": { "Buttonboard": {…}, "Mediaplayer1": {…}, … }
    /// Mit ConfigurationKeyName("") binden wir die direkten Unterknoten in <see cref="Entries"/>.
    /// </summary>
    public sealed class VlcOptions
    {
        [ConfigurationKeyName("")] // bindet alle direkten Kinder von "VLC" in dieses Dictionary
        [Required, MinLength(1)]
        public required Dictionary<string, VlcPlayerOptions> Entries { get; init; }
    }

    public sealed class VlcPlayerOptions
    {
        [Required] public required Uri BaseUri { get; init; }  // einheitlich wie OpenHAB
        [Required] public required string Password { get; init; }
    }

    public sealed class MqttOptions
    {
        [Required] public required string Server { get; init; }
        [Range(1, 65535)] public int Port { get; init; } = 1883;
        [Required] public required string Username { get; init; }
        [Required] public required string Password { get; init; }

        // Wenn du im Code Default-Fallbacks hast, kannst du Required entfernen und Defaults hier setzen.
        [Required] public required string WillTopic { get; init; }
        [Required] public required string OnlineTopic { get; init; }

        public IReadOnlyList<MqttDeviceOption>? Devices { get; init; }
    }

    public sealed class MqttDeviceOption
    {
        public string? Name { get; init; }
        public string? Topic { get; init; }
        public string? Reset { get; init; }
    }
}

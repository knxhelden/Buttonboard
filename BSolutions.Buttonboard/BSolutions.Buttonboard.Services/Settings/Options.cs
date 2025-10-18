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
        [Required] public required LyrionOptions Lyrion { get; init; }
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

    public sealed class LyrionOptions
    {
        public Uri BaseUri { get; set; } = new Uri("tcp://127.0.0.1:9090");
        public string? Username { get; set; }
        public string? Password { get; set; }

        public Dictionary<string, string> Players { get; set; } = new();
    }

    public sealed class VlcOptions
    {
        [ConfigurationKeyName("")]
        [Required, MinLength(1)]
        public required Dictionary<string, VlcPlayerOptions> Devices { get; init; }
    }

    public sealed class VlcPlayerOptions
    {
        [Required] public required Uri BaseUri { get; init; }
        [Required] public required string Password { get; init; }
    }

    public sealed class MqttOptions
    {
        [Required] public required string Server { get; init; }
        [Range(1, 65535)] public int Port { get; init; } = 1883;
        [Required] public required string Username { get; init; }
        [Required] public required string Password { get; init; }

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

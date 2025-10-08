// Services/Settings/Options.cs
using BSolutions.Buttonboard.Services.Gpio;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BSolutions.Buttonboard.Services.Settings
{

    public sealed class ButtonboardOptions
    {
        [Required] public required ApplicationOptions Application { get; init; }
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

    public sealed class OpenHabOptions
    {
        [Required] public required Uri BaseUri { get; init; }

        // Key = PlayerName ("Player1", "Player2", …)
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

    public sealed class VlcOptions
    {
        // Key = PlayerName ("Mediaplayer1", …)
        [Required, MinLength(1)]
        public required Dictionary<string, VlcPlayerOptions> Players { get; init; }
    }

    public sealed class VlcPlayerOptions
    {
        [Required] public required string BaseUri { get; init; }
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
    }


    public sealed class ScenarioOptions
    {
        [Required] public required SetupOptions Setup { get; init; } = new();
        [Required, MinLength(1)] public required List<SceneMap> Scenes { get; init; } = new();
    }

    public sealed class SetupOptions
    {
        [Required, MinLength(1)]
        public string Key { get; init; } = "setup";
    }

    public sealed class SceneMap
    {
        [Required, MinLength(1)]
        public string Key { get; set; } = "";

        [Required]
        public Button TriggerButton { get; set; }

        [Range(0, int.MaxValue)]
        public int RequiredStage { get; set; }
    }
}
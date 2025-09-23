using System;
using System.Collections.Generic;

namespace BSolutions.Buttonboard.Services.Settings
{
    public sealed class Application
    {
        public bool DisableSceneOrder { get; init; }
        public OperationMode OperationMode { get; set; } = OperationMode.Real;
        public required string ScenarioAssetsFolder { get; init; }
    }

    public sealed class OpenHAB
    {
        public required Uri BaseUri { get; init; }
        public required Audio Audio { get; init; }
    }

    public sealed class Audio
    {
        public List<AudioPlayer> Players { get; init; } = new();
    }

    public sealed class AudioPlayer
    {
        public required string Name { get; init; }
        public int Volume { get; init; }
        public required string StreamItem { get; init; }
        public required string VolumeItem { get; init; }
        public required string ControlItem { get; init; }
    }

    public sealed class VLC
    {
        public List<VLCPlayer> Players { get; init; } = new();
    }

    public sealed class VLCPlayer
    {
        public required string Name { get; init; }
        public required string BaseUri { get; init; }
        public required string Password { get; init; }
    }

    public sealed class Mqtt
    {
        public required string Server { get; init; }
        public int Port { get; init; }
        public required string Username { get; init; }
        public required string Password { get; init; }
        public required string WillTopic { get; init; }
        public required string OnlineTopic { get; init; }
    }
}

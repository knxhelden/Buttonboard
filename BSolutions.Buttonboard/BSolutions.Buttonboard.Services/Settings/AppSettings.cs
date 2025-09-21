using System;
using System.Collections.Generic;

namespace BSolutions.Buttonboard.Services.Settings
{
    public class Application
    {
        public bool TestOperation { get; set; }
        public string ScenesFolder { get; set; } = string.Empty;
    }

    public class OpenHAB
    {
        public Uri BaseUri { get; set; } = null!;
        public Audio Audio { get; set; } = new Audio();
    }

    public class Audio
    {
        public List<AudioPlayer> Players { get; set; } = new List<AudioPlayer>();
    }

    public class AudioPlayer
    {
        public string Name { get; set; } = string.Empty;
        public int Volume { get; set; }
        public string StreamItem { get; set; } = string.Empty;
        public string VolumeItem { get; set; } = string.Empty;
        public string ControlItem { get; set; } = string.Empty;
    }

    public class VLC
    {
        public List<VLCPlayer> Players = new List<VLCPlayer>();
    }

    public class VLCPlayer
    {
        public string Name { get; set; } = string.Empty;
        public string BaseUri { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class Mqtt
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string WillTopic { get; set; } = string.Empty;
        public string OnlineTopic { get; set; } = string.Empty;
    }
}

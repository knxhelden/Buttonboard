using System;
using System.Collections.Generic;

namespace BSolutions.Buttonboard.Services.Settings
{
    public class Application
    {
        public bool TestOperation { get; set; }
        public string ScenesFolder { get; set; }
    }

    public class OpenHAB
    {
        public Uri BaseUri { get; set; }
        public Audio Audio { get; set; } = new Audio();
    }

    public class Audio
    {
        public List<AudioPlayer> Players { get; set; } = new List<AudioPlayer>();
    }

    public class AudioPlayer
    {
        public string Name { get; set; }
        public int Volume { get; set; }
        public string StreamItem { get; set; }
        public string VolumeItem { get; set; }
        public string ControlItem { get; set; }
    }

    public class VLC
    {
        public List<VLCPlayer> Players = new List<VLCPlayer>();
    }

    public class VLCPlayer
    {
        public string Name { get; set; }
        public string BaseUri { get; set; }
        public string Password { get; set; }
    }

    public class Mqtt
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

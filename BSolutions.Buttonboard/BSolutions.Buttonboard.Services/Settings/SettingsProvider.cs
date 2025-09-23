using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace BSolutions.Buttonboard.Services.Settings
{
    public sealed class SettingsProvider : ISettingsProvider
    {
        private readonly IConfiguration _config;

        public Application Application { get; }
        public Audio Audio { get; }
        public OpenHAB OpenHAB { get; }
        public VLC VLC { get; }
        public Mqtt Mqtt { get; }

        public SettingsProvider(IConfiguration configuration)
        {
            _config = configuration;

            // Application
            var app = new Application
            {
                TestOperation = bool.TryParse(_config["Application:TestOperation"], out var test) && test,
                ScenarioAssetsFolder = _config["Application:ScenarioAssetsFolder"] ?? "assets",
                OperationMode = ParseEnumOrDefault(_config["Application:OperationMode"], OperationMode.Real)
            };

            // OpenHAB + Audio
            var ohBase = _config["OpenHAB:BaseUri"] ?? throw new InvalidOperationException("OpenHAB:BaseUri missing.");
            var audioPlayers = new List<AudioPlayer>();
            foreach (var s in _config.GetSection("OpenHAB:Audio").GetChildren())
            {
                audioPlayers.Add(new AudioPlayer
                {
                    Name = s.Key,
                    Volume = int.TryParse(s["Volume"], out var vol) ? vol : 0,
                    ControlItem = s["ControlItem"] ?? string.Empty,
                    StreamItem = s["StreamItem"] ?? string.Empty,
                    VolumeItem = s["VolumeItem"] ?? string.Empty
                });
            }

            var openhab = new OpenHAB
            {
                BaseUri = new Uri(ohBase),
                Audio = new Audio { Players = audioPlayers }
            };

            // VLC
            var vlcPlayers = new List<VLCPlayer>();
            foreach (var s in _config.GetSection("VLC").GetChildren())
            {
                vlcPlayers.Add(new VLCPlayer
                {
                    Name = s.Key,
                    BaseUri = s["BaseUri"] ?? string.Empty,
                    Password = s["Password"] ?? string.Empty
                });
            }

            var vlc = new VLC { Players = vlcPlayers };

            // MQTT
            var mqtt = new Mqtt
            {
                Server = _config["Mqtt:Server"] ?? string.Empty,
                Port = int.TryParse(_config["Mqtt:Port"], out var port) ? port : 1883,
                Username = _config["Mqtt:Username"] ?? string.Empty,
                Password = _config["Mqtt:Password"] ?? string.Empty,
                WillTopic = _config["Mqtt:WillTopic"] ?? string.Empty,
                OnlineTopic = _config["Mqtt:OnlineTopic"] ?? string.Empty
            };

            // Assign
            Application = app;
            OpenHAB = openhab;
            Audio = openhab.Audio;
            VLC = vlc;
            Mqtt = mqtt;
        }

        private static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum fallback)
            where TEnum : struct, Enum
        {
            return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;
        }
    }
}

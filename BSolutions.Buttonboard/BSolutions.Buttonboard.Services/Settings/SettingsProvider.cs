using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Settings
{
    public class SettingsProvider : ISettingsProvider
    {
        protected readonly IConfiguration _config;

        #region --- Properties ---

        public Application Application { get; set; }

        public Audio Audio { get; private set; } = new Audio();

        public OpenHAB OpenHAB { get; private set; }

        public VLC VLC { get; private set; } = new VLC();

        public Mqtt Mqtt { get; private set; }

        #endregion

        #region --- Constructor ---

        public SettingsProvider(IConfiguration configuration)
        {
            this._config = configuration;

            IConfigurationSection gpioSection = this._config.GetSection("Buttonboard").GetSection("GPIO");

            // Parse settings to object
            this.Application = new Application
            {
                TestOperation = Convert.ToBoolean(this._config.GetSection("Application").GetSection("TestOperation").Value)
            };

            // OpenHAB settings
            this.OpenHAB = new OpenHAB
            {
                BaseUri = new Uri(this._config.GetSection("OpenHAB").GetSection("BaseUri").Value)
            };

            foreach (var configItem in this._config.GetSection("OpenHAB").GetSection("Audio").GetChildren())
            {
                this.OpenHAB.Audio.Players.Add(new AudioPlayer
                {
                    Name = configItem.Key,
                    Volume = Convert.ToInt32(configItem.GetSection("Volume").Value),
                    ControlItem = configItem.GetSection("ControlItem").Value,
                    StreamItem = configItem.GetSection("StreamItem").Value,
                    VolumeItem = configItem.GetSection("VolumeItem").Value
                });
            }

            // VLC settings
            foreach(var configItem in this._config.GetSection("VLC").GetChildren())
            {
                this.VLC.Players.Add(new VLCPlayer
                {
                    Name = configItem.Key,
                    BaseUri = configItem.GetSection("BaseUri").Value,
                    Password = configItem.GetSection("Password").Value
                });
            }

            // MQTT settings
            this.Mqtt = new Mqtt
            {
                Server = this._config.GetSection("Mqtt").GetSection("Server").Value,
                Port = Convert.ToInt32(this._config.GetSection("Mqtt").GetSection("Port").Value),
                Username = this._config.GetSection("Mqtt").GetSection("Username").Value,
                Password = this._config.GetSection("Mqtt").GetSection("Password").Value,
                WillTopic = this._config.GetSection("Mqtt").GetSection("WillTopic").Value,
                OnlineTopic = this._config.GetSection("Mqtt").GetSection("OnlineTopic").Value
            };
        }

        #endregion
    }
}

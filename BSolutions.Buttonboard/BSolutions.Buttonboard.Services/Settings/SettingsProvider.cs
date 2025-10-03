using Microsoft.Extensions.Options;

namespace BSolutions.Buttonboard.Services.Settings
{
    public sealed class SettingsProvider : ISettingsProvider
    {
        public SettingsProvider(IOptions<ButtonboardOptions> options)
        {
            var o = options.Value;
            Application = o.Application;
            OpenHAB = o.OpenHAB;
            VLC = o.VLC;
            Mqtt = o.Mqtt;
        }

        public ApplicationOptions Application { get; }
        public OpenHabOptions OpenHAB { get; }
        public VlcOptions VLC { get; }
        public MqttOptions Mqtt { get; }
    }
}
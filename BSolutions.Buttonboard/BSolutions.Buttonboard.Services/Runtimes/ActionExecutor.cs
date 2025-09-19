using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public sealed class ActionExecutor : IActionExecutor
    {
        #region --- Fields ---

        protected readonly ILogger _logger;
        protected readonly ISettingsProvider _settings;
        protected readonly IOpenHabClient _openhab;
        protected readonly IVlcPlayerClient _vlc;
        protected readonly IMqttClient _mqtt;
        protected readonly IButtonboardGpioController _gpio;

        #endregion

        public ActionExecutor(ILogger<ActionExecutor> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IMqttClient mqtt)
        {
            _logger = logger;
            _settings = settingsProvider;

            _gpio = gpio;
            _openhab = openhab;
            _vlc = vlc;
            _mqtt = mqtt;
        }

        public async Task ExecuteAsync(SceneStep step, CancellationToken ct)
        {
            var a = step.Action?.ToLowerInvariant() ?? string.Empty;
            var args = step.Args;

            switch (a)
            {
                case "audio.play":
                    {
                        var url = args.GetString("url")
                            ?? throw new ArgumentException("audio.play requires 'url'");
                        var playerName = args.GetString("player", "Player1");

                        // Beispiel: URL starten
                        await _openhab.SendCommandAsync(_settings.OpenHAB.Audio.Players.Single(p => p.Name == playerName).StreamItem, url, ct);
                        break;
                    }

                case "video.next":
                    {
                        var playerName = args.GetString("player", "Mediaplayer1");
                        await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == playerName), ct);
                        break;
                    }

                case "video.pause":
                    {
                        var playerName = args.GetString("player", "Mediaplayer1");
                        await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == playerName), ct);
                        break;
                    }

                case "gpio.on":
                    {
                        var pin = args.GetString("pin")
                            ?? throw new ArgumentException("gpio.on requires 'pin'");
                        await _gpio.LedOnAsync(ParseLed(pin), ct);
                        break;
                    }

                case "gpio.off":
                    {
                        var pin = args.GetString("pin")
                            ?? throw new ArgumentException("gpio.off requires 'pin'");
                        await _gpio.LedOffAsync(ParseLed(pin), ct);
                        break;
                    }

                case "gpio.blink":
                    {
                        var count = args.GetInt("count", 3);
                        var interval = args.GetInt("intervalMs", 100);
                        await _gpio.LedsBlinkingAsync(count, interval, ct);
                        break;
                    }

                case "mqtt.pub":
                    {
                        var topic = args.GetString("topic")
                            ?? throw new ArgumentException("mqtt.pub requires 'topic'");

                        // payload kann String ODER Objekt/Array sein:
                        var payloadNode = args.GetNode("payload");
                        var payload = payloadNode is JsonElement el ? el.GetRawText() : args.GetString("payload", "ON");

                        await _mqtt.PublishAsync(topic, payload);
                        break;
                    }

                default:
                    _logger.LogWarning($"Unknown action '{a}'");
                    break;
            }
        }

        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led)) return led;
            throw new ArgumentException($"Unknown Led '{s}'");
        }
    }
}

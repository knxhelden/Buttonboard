using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Default implementation of <see cref="IActionExecutor"/>.
    /// Dispatches <see cref="ScenarioAssetStep"/> actions to concrete subsystems (OpenHAB/VLC/GPIO/MQTT).
    /// Uses structured logging with stable EventIds and honors cooperative cancellation.
    /// </summary>
    public sealed class ActionExecutor : IActionExecutor
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IOpenHabClient _openhab;
        private readonly IVlcPlayerClient _vlc;
        private readonly IMqttClient _mqtt;
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Creates a new <see cref="ActionExecutor"/>.
        /// </summary>
        public ActionExecutor(
            ILogger<ActionExecutor> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IMqttClient mqtt)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
            _openhab = openhab ?? throw new ArgumentNullException(nameof(openhab));
            _vlc = vlc ?? throw new ArgumentNullException(nameof(vlc));
            _mqtt = mqtt ?? throw new ArgumentNullException(nameof(mqtt));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            if (step is null) throw new ArgumentNullException(nameof(step));

            // Normalize action string once
            var action = step.Action?.Trim();
            var a = action?.ToLowerInvariant() ?? string.Empty;
            var args = step.Args;

            switch (a)
            {
                case "audio.play":
                    {
                        var url = args.GetString("url");
                        if (string.IsNullOrWhiteSpace(url))
                        {
                            _logger.LogWarning(LogEvents.ExecArgMissing, "audio.play requires argument {Arg}", "url");
                            throw new ArgumentException("audio.play requires 'url'");
                        }

                        var playerName = args.GetString("player", "Player1");

                        // NEU: Dictionary-Lookup statt .Players.FirstOrDefault(...)
                        if (!_settings.OpenHAB.Audio.TryGetValue(playerName, out var player))
                        {
                            _logger.LogWarning(LogEvents.ExecResourceMissing,
                                "audio.play: OpenHAB player not found {Player}", playerName);
                            throw new ArgumentException($"Unknown OpenHAB audio player '{playerName}'");
                        }

                        _logger.LogInformation(LogEvents.ExecAudioPlay,
                            "audio.play: sending URL to player {Player} (Item {StreamItem})",
                            playerName, player.StreamItem);

                        await _openhab.SendCommandAsync(player.StreamItem, url, ct).ConfigureAwait(false);
                        return;
                    }

                case "video.next":
                    {
                        var playerName = args.GetString("player", "Mediaplayer1");

                        // NEU: Dictionary-Lookup
                        if (!_settings.VLC.Players.TryGetValue(playerName, out var _))
                        {
                            _logger.LogWarning(LogEvents.ExecResourceMissing,
                                "video.next: VLC player not found {Player}", playerName);
                            throw new ArgumentException($"Unknown VLC player '{playerName}'");
                        }

                        _logger.LogInformation(LogEvents.ExecVideoNext,
                            "video.next: issuing NEXT to {Player}", playerName);

                        // NEU: API erwartet string statt Objekt
                        await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct).ConfigureAwait(false);
                        return;
                    }

                case "video.pause":
                    {
                        var playerName = args.GetString("player", "Mediaplayer1");

                        if (!_settings.VLC.Players.TryGetValue(playerName, out var _))
                        {
                            _logger.LogWarning(LogEvents.ExecResourceMissing,
                                "video.pause: VLC player not found {Player}", playerName);
                            throw new ArgumentException($"Unknown VLC player '{playerName}'");
                        }

                        _logger.LogInformation(LogEvents.ExecVideoPause,
                            "video.pause: issuing PAUSE to {Player}", playerName);

                        await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, playerName, ct).ConfigureAwait(false);
                        return;
                    }

                case "gpio.on":
                    {
                        var ledStr = args.GetString("led");
                        if (string.IsNullOrWhiteSpace(ledStr))
                        {
                            _logger.LogWarning(LogEvents.ExecArgMissing,
                                "gpio.on requires argument {Arg}", "led");
                            throw new ArgumentException("gpio.on requires 'led'");
                        }

                        var led = ParseLed(ledStr);
                        _logger.LogInformation(LogEvents.ExecGpioOn,
                            "gpio.on: setting LED {Pin} ON", led);

                        await _gpio.LedOnAsync(led, ct).ConfigureAwait(false);
                        return;
                    }

                case "gpio.off":
                    {
                        var ledStr = args.GetString("led");
                        if (string.IsNullOrWhiteSpace(ledStr))
                        {
                            _logger.LogWarning(LogEvents.ExecArgMissing,
                                "gpio.off requires argument {Arg}", "led");
                            throw new ArgumentException("gpio.off requires 'led'");
                        }

                        var led = ParseLed(ledStr);
                        _logger.LogInformation(LogEvents.ExecGpioOff,
                            "gpio.off: setting LED {Pin} OFF", led);

                        await _gpio.LedOffAsync(led, ct).ConfigureAwait(false);
                        return;
                    }

                case "gpio.blink":
                    {
                        var count = args.GetInt("count", 3);
                        var interval = args.GetInt("intervalMs", 100);

                        _logger.LogInformation(LogEvents.ExecGpioBlink,
                            "gpio.blink: blinking LEDs Count {Count} IntervalMs {IntervalMs}",
                            count, interval);

                        await _gpio.LedsBlinkingAsync(count, interval, ct).ConfigureAwait(false);
                        return;
                    }

                case "mqtt.pub":
                    {
                        var topic = args.GetString("topic");
                        if (string.IsNullOrWhiteSpace(topic))
                        {
                            _logger.LogWarning(LogEvents.ExecArgMissing,
                                "mqtt.pub requires argument {Arg}", "topic");
                            throw new ArgumentException("mqtt.pub requires 'topic'");
                        }

                        // Payload can be string OR JSON
                        var payloadNode = args.GetNode("payload");

                        string payload;
                        if (payloadNode is JsonElement el)
                        {
                            // Strings as plain text, otherwise raw JSON
                            payload = el.ValueKind == JsonValueKind.String
                                ? (el.GetString() ?? string.Empty)
                                : el.GetRawText();
                        }
                        else
                        {
                            // Fallback to simple string
                            payload = args.GetString("payload", "ON") ?? string.Empty;
                        }

                        _logger.LogInformation(LogEvents.ExecMqttPublish,
                            "mqtt.pub: publishing to {Topic} (PayloadLength {Length})",
                            topic, payload.Length);

                        await _mqtt.PublishAsync(topic, payload, ct).ConfigureAwait(false);
                        return;
                    }


                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown action {Action}", action ?? "(null)");
                    // Decide: either ignore silently or throw. Keeping warning+return keeps runtime lenient.
                    return;
            }
        }

        /// <summary>
        /// Parses a string into a <see cref="Led"/> enumeration value.
        /// Throws if the input cannot be mapped.
        /// </summary>
        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led))
                return led;

            throw new ArgumentException($"Unknown Led '{s}'", nameof(s));
        }
    }
}

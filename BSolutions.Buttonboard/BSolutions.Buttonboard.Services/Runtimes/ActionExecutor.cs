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
    /// <summary>
    /// Default implementation of <see cref="IActionExecutor"/>.
    /// <para>
    /// Dispatches <see cref="ScenarioAssetStep"/> actions to concrete subsystems such as:
    /// <list type="bullet">
    ///   <item><b>OpenHAB</b> for audio commands</item>
    ///   <item><b>VLC</b> for video commands</item>
    ///   <item><b>GPIO</b> for LED control</item>
    ///   <item><b>MQTT</b> for message publishing</item>
    /// </list>
    /// </para>
    /// </summary>
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

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="ActionExecutor"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output and warnings.</param>
        /// <param name="settingsProvider">Provides access to configured players, pins, and system settings.</param>
        /// <param name="gpio">GPIO controller for LED operations.</param>
        /// <param name="openhab">REST client for sending OpenHAB commands.</param>
        /// <param name="vlc">Client for controlling VLC media players.</param>
        /// <param name="mqtt">Client for publishing MQTT messages.</param>
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

        #endregion

        #region --- IActionExecutor ---

        /// <inheritdoc />
        /// <summary>
        /// Executes the specified <paramref name="step"/> by dispatching it to the appropriate subsystem.
        /// </summary>
        /// <param name="step">Scenario asset step to execute (defines <c>Action</c> and <c>Args</c>).</param>
        /// <param name="ct">Cancellation token to abort long-running operations.</param>
        /// <remarks>
        /// Supported <c>Action</c> values:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Action</term>
        ///     <description>Behavior</description>
        ///   </listheader>
        ///   <item>
        ///     <term><c>audio.play</c></term>
        ///     <description>Sends a stream URL to an OpenHAB audio player (<c>args: url, player</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>video.next</c></term>
        ///     <description>Sends "next" to a VLC media player (<c>args: player</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>video.pause</c></term>
        ///     <description>Sends "pause" to a VLC media player (<c>args: player</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>gpio.on</c></term>
        ///     <description>Turns on the specified LED (<c>args: pin</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>gpio.off</c></term>
        ///     <description>Turns off the specified LED (<c>args: pin</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>gpio.blink</c></term>
        ///     <description>Blinks all LEDs (<c>args: count, intervalMs</c>).</description>
        ///   </item>
        ///   <item>
        ///     <term><c>mqtt.pub</c></term>
        ///     <description>Publishes a message to an MQTT topic (<c>args: topic, payload</c>).</description>
        ///   </item>
        /// </list>
        /// Unknown actions are logged as warnings and ignored.
        /// </remarks>
        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
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

        #endregion

        /// <summary>
        /// Parses a string into a <see cref="Led"/> enumeration value.
        /// Throws if the input cannot be mapped.
        /// </summary>
        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led)) return led;
            throw new ArgumentException($"Unknown Led '{s}'");
        }
    }
}

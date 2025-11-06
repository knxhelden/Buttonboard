using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.MqttClients;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Routes and executes MQTT-related actions such as <c>mqtt.pub</c>.
    /// </summary>
    /// <remarks>
    /// This router provides a unified interface for publishing messages to MQTT topics.
    /// It uses the injected <see cref="IMqttClient"/> abstraction to send payloads 
    /// to the broker defined in the runtime configuration.
    ///
    /// Supported operations:
    /// <list type="bullet">
    /// <item><description><c>mqtt.pub</c> – Publishes a message to the specified MQTT topic.</description></item>
    /// </list>
    /// </remarks>
    public sealed class MqttActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly IMqttClient _mqtt;

        /// <inheritdoc />
        public string Domain => "mqtt";

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttActionRouter"/> class.
        /// </summary>
        /// <param name="logger">The logger used for structured runtime diagnostics.</param>
        /// <param name="mqtt">The MQTT client used to send publish commands.</param>
        public MqttActionRouter(
            ILogger<MqttActionRouter> logger,
            IMqttClient mqtt)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mqtt = mqtt ?? throw new ArgumentNullException(nameof(mqtt));
        }

        #endregion

        #region --- IActionRouter ---

        /// <inheritdoc />
        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            switch (op)
            {
                case "pub":
                    await HandlePublishAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown MQTT action {Action}", key);
                    break;
            }
        }

        #endregion

        #region --- Handlers ---

        /// <summary>
        /// Handles the <c>mqtt.pub</c> operation.
        /// Publishes a message to a given MQTT topic with an optional payload.
        /// </summary>
        /// <param name="step">
        /// The scenario step containing:
        /// <list type="bullet">
        /// <item><term><c>topic</c></term><description>The MQTT topic to publish to (required).</description></item>
        /// <item><term><c>payload</c></term><description>The message payload (optional; string or JSON node).</description></item>
        /// </list>
        /// </param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        /// <exception cref="ArgumentException">Thrown when <c>topic</c> is missing or invalid.</exception>
        private async Task HandlePublishAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var args = step.Args;

            var topic = args.GetString("topic");
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing,
                    "mqtt.pub requires argument {Arg}", "topic");
                throw new ArgumentException("mqtt.pub requires 'topic'");
            }

            // Payload can be a string or a JSON node
            var payloadNode = args.GetNode("payload");
            string payload;

            if (payloadNode is JsonElement el)
            {
                payload = el.ValueKind == JsonValueKind.String
                    ? (el.GetString() ?? string.Empty)
                    : el.GetRawText();
            }
            else
            {
                payload = args.GetString("payload", "ON") ?? string.Empty;
            }

            _logger.LogInformation(LogEvents.ExecMqttPublish,
                "mqtt.pub: publishing to {Topic} (PayloadLength {Length})",
                topic, payload.Length);

            await _mqtt.PublishAsync(topic, payload, ct).ConfigureAwait(false);
        }

        #endregion
    }
}

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
    public sealed class MqttPublishHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly IMqttClient _mqtt;

        public string Key => "mqtt.pub";

        public MqttPublishHandler(ILogger<MqttPublishHandler> logger,
                                  IMqttClient mqtt)
        {
            _logger = logger;
            _mqtt = mqtt;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;

            var topic = args.GetString("topic");
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing,
                    "mqtt.pub requires argument {Arg}", "topic");
                throw new ArgumentException("mqtt.pub requires 'topic'");
            }

            // payload: string OR JSON node
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
    }
}

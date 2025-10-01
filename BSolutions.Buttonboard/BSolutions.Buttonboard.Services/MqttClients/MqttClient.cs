using System;
using System.Threading;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    /// <summary>
    /// MQTT client implementation based on <see cref="IManagedMqttClient"/> from the MQTTnet library.
    /// Uses managed options for auto-reconnect and offline queueing.
    /// </summary>
    public sealed class MqttClient : IMqttClient, IDisposable
    {
        private readonly ILogger<MqttClient> _logger;
        private readonly IManagedMqttClient _client;
        private readonly ManagedMqttClientOptions _managedOptions;
        private readonly string _onlineTopic;
        private readonly string _willTopic;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClient"/> class with options
        /// derived from application settings.
        /// </summary>
        public MqttClient(ISettingsProvider settings, ILogger<MqttClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var factory = new MqttFactory();
            _client = factory.CreateManagedMqttClient();

            _onlineTopic = string.IsNullOrWhiteSpace(settings.Mqtt.OnlineTopic)
                ? "buttonboard/status"
                : settings.Mqtt.OnlineTopic!;
            _willTopic = string.IsNullOrWhiteSpace(settings.Mqtt.WillTopic)
                ? "buttonboard/status"
                : settings.Mqtt.WillTopic!;

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(settings.Mqtt.Server, settings.Mqtt.Port)
                .WithCredentials(settings.Mqtt.Username, settings.Mqtt.Password)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithWillTopic(_willTopic)
                .WithWillPayload("offline")
                .WithWillRetain()
                .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            _managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions)
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .Build();

            // Hook lifecycle events for observability
            _client.ConnectedAsync += async e =>
            {
                _logger.LogInformation(LogEvents.MqttConnected,
                    "MQTT connected: Server {Server}:{Port} SessionPresent {SessionPresent}",
                    settings.Mqtt.Server, settings.Mqtt.Port, e.ConnectResult?.IsSessionPresent);

                // Publish "online" announcement
                try
                {
                    var onlineMsg = new MqttApplicationMessageBuilder()
                        .WithTopic(_onlineTopic)
                        .WithPayload("online")
                        .WithRetainFlag()
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _client.EnqueueAsync(onlineMsg).ConfigureAwait(false);
                    _logger.LogInformation(LogEvents.MqttOnlineAnnounced,
                        "MQTT online announced to {Topic}", _onlineTopic);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(LogEvents.MqttError, ex,
                        "Failed to enqueue online announcement to {Topic}", _onlineTopic);
                }
            };

            _client.DisconnectedAsync += e =>
            {
                // MQTTnet v4 sets Reason on server disconnects; fall back to exception message
                var reason = e.Reason?.ToString() ?? e.Exception?.Message ?? "unknown";
                _logger.LogWarning(LogEvents.MqttDisconnected,
                    "MQTT disconnected: Reason {Reason}", reason);
                return Task.CompletedTask;
            };

            _client.ConnectingFailedAsync += e =>
            {
                _logger.LogWarning(LogEvents.MqttConnectFailed,
                    "MQTT connect failed: {Error}", e.Exception?.Message ?? "unknown");
                return Task.CompletedTask;
            };
        }

        /// <inheritdoc />
        public async Task ConnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClient));

            _logger.LogInformation(LogEvents.MqttConnecting, "Starting MQTT managed client");
            try
            {
                await _client.StartAsync(_managedOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Managed client will keep retrying; still log for visibility.
                _logger.LogWarning(LogEvents.MqttError, ex, "StartAsync threw an exception (auto-reconnect will continue)");
            }
        }

        /// <inheritdoc />
        public async Task PublishAsync(string topic, string payload, CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClient));
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning(LogEvents.MqttInvalidTopic, "Publish skipped: empty topic");
                throw new ArgumentException("Topic must not be null or whitespace.", nameof(topic));
            }
            if (ct.IsCancellationRequested) return;

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload ?? string.Empty)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _client.EnqueueAsync(msg).ConfigureAwait(false);
                _logger.LogInformation(LogEvents.MqttPublishEnqueued,
                    "MQTT publish enqueued Topic {Topic} PayloadLength {Length} Pending {Pending}",
                    topic, (payload?.Length ?? 0), _client.PendingApplicationMessagesCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(LogEvents.MqttPublishDropped,
                    "MQTT publish canceled Topic {Topic}", topic);
                throw;
            }
            catch (Exception ex)
            {
                // Do not crash the app; report and continue.
                _logger.LogWarning(LogEvents.MqttPublishDropped, ex,
                    "MQTT publish enqueue failed Topic {Topic} (PayloadLength {Length})",
                    topic, (payload?.Length ?? 0));
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_disposed) return;

            _logger.LogInformation(LogEvents.MqttStopping,
                "Stopping MQTT client. Pending {Pending}", _client.PendingApplicationMessagesCount);

            // Try to drain pending messages (bounded)
            var deadline = DateTime.UtcNow.AddSeconds(5);
            try
            {
                while (_client.PendingApplicationMessagesCount > 0 &&
                       DateTime.UtcNow < deadline &&
                       !ct.IsCancellationRequested)
                {
                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(LogEvents.MqttPendingDrained,
                    "Pending queue drained (or timeout). Remaining {Pending}",
                    _client.PendingApplicationMessagesCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(LogEvents.MqttPendingDrained,
                    "Pending drain canceled. Remaining {Pending}",
                    _client.PendingApplicationMessagesCount);
            }

            // Stop managed client (disconnect + worker shutdown)
            try
            {
                await _client.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogEvents.MqttError, ex, "StopAsync threw an exception");
            }

            _logger.LogInformation(LogEvents.MqttStopped, "MQTT client stopped");
        }

        /// <summary>
        /// Disposes the client, ensuring shutdown and resource cleanup.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Best-effort synchronous stop
                _client?.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogEvents.MqttError, ex, "Dispose encountered errors during StopAsync");
            }
            finally
            {
                _client?.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}

using BSolutions.Buttonboard.Services.Settings;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    public sealed class MqttClient : IMqttClient, IDisposable
    {
        private readonly IManagedMqttClient _client;
        private readonly ManagedMqttClientOptions _options;
        private bool _disposed;

        public MqttClient(ISettingsProvider settings)
        {
            var factory = new MqttFactory();
            _client = factory.CreateManagedMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(settings.Mqtt.Server, settings.Mqtt.Port)
                .WithCredentials(settings.Mqtt.Username, settings.Mqtt.Password)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithWillTopic(settings.Mqtt.WillTopic ?? "buttonboard/status")
                .WithWillPayload("offline")
                .WithWillRetain()
                .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            _options = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions)
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .Build();


            _client.ConnectedAsync += _ =>
            {
                return _client.EnqueueAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(settings.Mqtt.OnlineTopic ?? "buttonboard/status")
                    .WithPayload("online")
                    .WithRetainFlag()
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());
            };
        }

        /// <summary>
        /// Starts the managed client. Will auto-reconnect in the background.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClient));
            try { await _client.StartAsync(_options); }
            catch { /* swallow – ManagedClient reconnects in background */ }
        }

        /// <summary>
        /// Publishes a message. If offline, it will be queued and sent once connected.
        /// </summary>
        public async Task PublishAsync(string topic, string payload, CancellationToken ct = default)
        {
            if (_disposed || ct.IsCancellationRequested) return;

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try { await _client.EnqueueAsync(msg); }
            catch { /* swallow – do not crash the app */ }
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_disposed) return;

            // 1) Short flush phase: wait until pending queue is empty (or timeout)
            var deadline = DateTime.UtcNow.AddSeconds(5);
            try
            {
                while (_client.PendingApplicationMessagesCount > 0 &&
                       DateTime.UtcNow < deadline &&
                       !ct.IsCancellationRequested)
                {
                    await Task.Delay(100, ct);
                }
            }
            catch (OperationCanceledException) { /* ok on shutdown */ }

            // 2) Stop managed client (disconnect + stop worker)
            try { await _client.StopAsync(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _client?.StopAsync().GetAwaiter().GetResult(); } catch { /* ignore */ }
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

using BSolutions.Buttonboard.Services.Settings;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    public sealed class ButtonboardMqttClient : IButtonboardMqttClient
    {
        private readonly IManagedMqttClient _client;
        private readonly ManagedMqttClientOptions _options;
        private bool _disposed;

        public ButtonboardMqttClient(ISettingsProvider settingsProvider)
        {
            if (settingsProvider == null) throw new ArgumentNullException(nameof(settingsProvider));

            var factory = new MqttFactory();
            _client = factory.CreateManagedMqttClient();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(settingsProvider.Mqtt.Server, settingsProvider.Mqtt.Port)
                .WithCredentials(settingsProvider.Mqtt.Username, settingsProvider.Mqtt.Password)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();

            _options = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions)
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .Build();

            // Events (nur Logging, keine Exceptions nach außen werfen)
            _client.ConnectedAsync += e =>
            {
                Console.WriteLine($"[MQTT] Connected to broker '{settingsProvider.Mqtt.Server}:{settingsProvider.Mqtt.Port}'.");
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += e =>
            {
                Console.WriteLine($"[MQTT] Disconnected: {e.Reason} {e.Exception?.Message}");
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Starts the managed client. Will auto-reconnect in the background.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ButtonboardMqttClient));

            try
            {
                await _client.StartAsync(_options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] StartAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes a message. If offline, it will be queued and sent once connected.
        /// </summary>
        public async Task PublishAsync(string topic, string payload)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ButtonboardMqttClient));

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _client.EnqueueAsync(msg); // nur 1 Argument in v4!
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] Enqueue failed: {ex.Message}");
            }
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

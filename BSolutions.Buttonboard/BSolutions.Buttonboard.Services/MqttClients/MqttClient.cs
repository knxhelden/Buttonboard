using BSolutions.Buttonboard.Services.Settings;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    /// <summary>
    /// MQTT client implementation based on <see cref="IManagedMqttClient"/> from the MQTTnet library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The client uses <see cref="ManagedMqttClientOptions"/> to provide automatic reconnection and 
    /// message queuing. Messages published while offline are enqueued and delivered once a connection
    /// is re-established.
    /// </para>
    /// <para>
    /// Connection details (server, port, credentials, topics) are provided by <see cref="ISettingsProvider"/>.
    /// A "last will" message (<c>offline</c>) is configured on the <c>WillTopic</c>. When successfully connected,
    /// the client automatically publishes an <c>online</c> message to the <c>OnlineTopic</c>.
    /// </para>
    /// <para>
    /// The client is disposable. After disposal, further calls to <see cref="ConnectAsync"/> or 
    /// <see cref="PublishAsync"/> will be ignored or throw <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public sealed class MqttClient : IMqttClient, IDisposable
    {
        private readonly IManagedMqttClient _client;
        private readonly ManagedMqttClientOptions _options;
        private bool _disposed;

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClient"/> class with options
        /// derived from application settings.
        /// </summary>
        /// <param name="settings">
        /// Provides MQTT configuration (server, port, username, password, topics).
        /// </param>
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

        #endregion

        /// <summary>
        /// Starts the managed MQTT client. 
        /// </summary>
        /// <remarks>
        /// The client will attempt to auto-reconnect in the background if the initial 
        /// connection or a later connection attempt fails.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the client has already been disposed.</exception>
        public async Task ConnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClient));
            try { await _client.StartAsync(_options); }
            catch { /* swallow – ManagedClient reconnects in background */ }
        }

        /// <summary>
        /// Publishes a message to a given topic.
        /// </summary>
        /// <remarks>
        /// If the client is not currently connected, the message is enqueued and sent once 
        /// a connection is available. Messages are sent with QoS 1 (at-least-once).
        /// </remarks>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="payload">The message payload (UTF-8 encoded).</param>
        /// <param name="ct">Cancellation token for aborting the publish operation.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the client has already been disposed.</exception>
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

        /// <summary>
        /// Stops the client and attempts a graceful shutdown.
        /// </summary>
        /// <remarks>
        /// <list type="number">
        ///   <item><description>Flush pending messages for up to 5 seconds or until <paramref name="ct"/> cancels.</description></item>
        ///   <item><description>Stop the managed client (disconnect + worker shutdown).</description></item>
        /// </list>
        /// </remarks>
        /// <param name="ct">Cancellation token for aborting the stop operation.</param>
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

        /// <summary>
        /// Disposes the client, ensuring shutdown and resource cleanup.
        /// </summary>
        /// <remarks>
        /// Invokes <see cref="StopAsync(CancellationToken)"/> synchronously during disposal.
        /// </remarks>
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

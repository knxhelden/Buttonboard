using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    /// <summary>
    /// In-memory mock implementation of <see cref="IMqttClient"/> for tests and local development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mock does not connect to a real MQTT broker. Instead, it simulates connection,
    /// logs all operations, and stores published messages in memory.
    /// </para>
    /// <para>
    /// Behavior:
    /// <list type="bullet">
    ///   <item><description><see cref="ConnectAsync"/> simply marks the client as connected.</description></item>
    ///   <item><description><see cref="PublishAsync"/> logs the message and stores it in an in-memory queue per topic.</description></item>
    ///   <item><description><see cref="StopAsync"/> clears the connected flag and flushes the queues.</description></item>
    /// </list>
    /// Artificial delays (10–25 ms) are applied to mimic async I/O.
    /// </para>
    /// </remarks>
    public sealed class MqttClientMock : IMqttClient, IDisposable
    {
        private readonly ILogger<MqttClientMock> _logger;
        private readonly ConcurrentDictionary<string, List<string>> _messages = new(StringComparer.OrdinalIgnoreCase);
        private bool _connected;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientMock"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and simulation traces.</param>
        public MqttClientMock(ILogger<MqttClientMock> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ConnectAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClientMock));
            await Task.Delay(25).ConfigureAwait(false); // simulate latency
            _connected = true;
            _logger.LogInformation("[SIM/MQTT] Connected to broker (mock).");
        }

        /// <inheritdoc />
        public async Task PublishAsync(string topic, string payload, CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClientMock));
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("MQTT topic must not be null or whitespace.", nameof(topic));

            await Task.Delay(10, ct).ConfigureAwait(false); // simulate latency

            if (!_connected)
            {
                _logger.LogWarning("[SIM/MQTT] Publish while disconnected. Topic={Topic}, Payload={Payload}", topic, payload);
                return;
            }

            _logger.LogInformation("[SIM/MQTT] Publish -> Topic: {Topic}, Payload: {Payload}", topic, payload);

            // Store in memory
            _messages.AddOrUpdate(
                topic,
                _ => new List<string> { payload },
                (_, list) =>
                {
                    list.Add(payload);
                    return list;
                });
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_disposed) return;
            await Task.Delay(20, ct).ConfigureAwait(false); // simulate latency
            _connected = false;
            _logger.LogInformation("[SIM/MQTT] Disconnected from broker (mock).");
        }

        /// <summary>
        /// Retrieves all messages published to a given topic during the mock lifetime.
        /// </summary>
        /// <param name="topic">The topic to query.</param>
        /// <returns>A copy of the message list for the topic, or empty list if none.</returns>
        public IReadOnlyList<string> GetMessages(string topic)
        {
            if (_messages.TryGetValue(topic, out var list))
            {
                return list.ToArray();
            }
            return Array.Empty<string>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _messages.Clear();
            _logger.LogDebug("[SIM/MQTT] Mock disposed.");
        }
    }
}

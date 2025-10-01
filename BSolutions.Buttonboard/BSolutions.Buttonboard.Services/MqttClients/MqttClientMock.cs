using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    /// <summary>
    /// In-memory mock of <see cref="IMqttClient"/> for tests and local development.
    /// Simulates connect/publish/stop without a real broker and stores messages per topic.
    /// </summary>
    public sealed class MqttClientMock : IMqttClient, IDisposable
    {
        private readonly ILogger<MqttClientMock> _logger;

        // Per-topic queues to be thread-safe under concurrent publishes.
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _messages =
            new(StringComparer.OrdinalIgnoreCase);

        private volatile bool _connected;
        private volatile bool _disposed;

        /// <summary>
        /// Creates a new <see cref="MqttClientMock"/>.
        /// </summary>
        public MqttClientMock(ILogger<MqttClientMock> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ConnectAsync()
        {
            ThrowIfDisposed();

            _logger.LogInformation(LogEvents.MqttConnecting, "Starting MQTT mock client");
            await Task.Delay(25).ConfigureAwait(false); // simulate latency

            _connected = true;
            _logger.LogInformation(LogEvents.MqttConnected,
                "MQTT mock connected (no real broker)");
        }

        /// <inheritdoc />
        public async Task PublishAsync(string topic, string payload, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning(LogEvents.MqttInvalidTopic, "Publish skipped: empty topic");
                throw new ArgumentException("Topic must not be null or whitespace.", nameof(topic));
            }
            if (ct.IsCancellationRequested) return;

            await Task.Delay(10, ct).ConfigureAwait(false); // simulate latency

            if (!_connected)
            {
                _logger.LogWarning(LogEvents.MqttPublishDropped,
                    "Publish while disconnected Topic {Topic} PayloadLength {Length}",
                    topic, payload?.Length ?? 0);
                return;
            }

            var q = _messages.GetOrAdd(topic, _ => new ConcurrentQueue<string>());
            q.Enqueue(payload ?? string.Empty);

            var pending = _messages.Sum(kvp => kvp.Value.Count);
            _logger.LogInformation(LogEvents.MqttPublishEnqueued,
                "MQTT mock publish enqueued Topic {Topic} PayloadLength {Length} Pending {Pending}",
                topic, (payload?.Length ?? 0), pending);
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken ct = default)
        {
            if (_disposed) return;

            _logger.LogInformation(LogEvents.MqttStopping,
                "Stopping MQTT mock client. Pending {Pending}", TotalPending());

            // Simulate a short drain phase (mock simply waits; does not forward anywhere)
            var deadline = DateTime.UtcNow.AddMilliseconds(250);
            try
            {
                while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                {
                    await Task.Delay(25, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // fine – shutdown bounded by caller
            }

            _connected = false;
            _logger.LogInformation(LogEvents.MqttStopped, "MQTT mock client stopped");
        }

        /// <summary>
        /// Returns a snapshot of all messages published to a given topic.
        /// </summary>
        public IReadOnlyList<string> GetMessages(string topic)
        {
            if (topic is null) throw new ArgumentNullException(nameof(topic));
            return _messages.TryGetValue(topic, out var q)
                ? q.ToArray()
                : Array.Empty<string>();
        }

        /// <summary>
        /// Clears all stored messages for a given topic (test helper).
        /// </summary>
        public void ClearTopic(string topic)
        {
            if (topic is null) throw new ArgumentNullException(nameof(topic));
            _messages.TryRemove(topic, out _);
        }

        /// <summary>
        /// Clears all stored messages across all topics (test helper).
        /// </summary>
        public void ClearAll() => _messages.Clear();

        /// <summary>
        /// Disposes the mock and clears all state.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _connected = false;
            _messages.Clear();
            _logger.LogDebug("MQTT mock disposed");
        }

        private int TotalPending() => _messages.Sum(kvp => kvp.Value.Count);

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MqttClientMock));
        }
    }
}

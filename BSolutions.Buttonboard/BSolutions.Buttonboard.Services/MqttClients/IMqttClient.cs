using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    /// <summary>
    /// Defines a minimal MQTT client contract for connecting, publishing messages, and graceful shutdown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations SHOULD be resilient: they SHOULD handle connectivity loss, attempt automatic reconnection,
    /// and support best-effort delivery with an internal queue while offline.
    /// </para>
    /// <para>
    /// Payload encoding is expected to be UTF-8 unless documented otherwise. At-least-once delivery (QoS 1)
    /// is RECOMMENDED for <see cref="PublishAsync(string, string, CancellationToken)"/>.
    /// </para>
    /// <para>
    /// Thread-safety: Implementations SHOULD allow concurrent calls to all methods.
    /// </para>
    /// </remarks>
    public interface IMqttClient
    {
        /// <summary>
        /// Establishes a connection to the MQTT broker using the implementation's configuration.
        /// Implementations MAY enable automatic reconnection in the background.
        /// </summary>
        /// <returns>A task that completes when the client has started its connection workflow.</returns>
        /// <exception cref="System.ObjectDisposedException">Thrown if the client was disposed.</exception>
        Task ConnectAsync();

        /// <summary>
        /// Publishes a message to a given <paramref name="topic"/>.
        /// If the client is temporarily offline, implementations SHOULD enqueue the message
        /// and send it once a connection is available.
        /// </summary>
        /// <param name="topic">The MQTT topic to publish to (non-empty, valid per MQTT spec).</param>
        /// <param name="payload">The message payload (commonly UTF-8 text or JSON).</param>
        /// <param name="ct">Cancellation token for aborting the publish operation.</param>
        /// <returns>
        /// A task that completes when the publish request has been accepted by the client
        /// (not necessarily delivered to the broker yet).
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="topic"/> is null or whitespace.</exception>
        /// <exception cref="System.ObjectDisposedException">Thrown if the client was disposed.</exception>
        Task PublishAsync(string topic, string payload, CancellationToken ct = default);

        /// <summary>
        /// Stops the client and releases underlying resources gracefully.
        /// Implementations SHOULD attempt to flush any pending messages before disconnecting,
        /// honoring <paramref name="ct"/> to bound the shutdown time.
        /// </summary>
        /// <param name="ct">Cancellation token for aborting the stop operation.</param>
        /// <returns>A task that completes when the client has been stopped.</returns>
        /// <exception cref="System.OperationCanceledException">Thrown if the operation is canceled.</exception>
        Task StopAsync(CancellationToken ct = default);
    }
}

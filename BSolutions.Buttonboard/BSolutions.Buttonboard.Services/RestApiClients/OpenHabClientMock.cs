using BSolutions.Buttonboard.Services.Enumerations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// In-memory mock of <see cref="IOpenHabClient"/> for tests and offline development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mock does not perform any HTTP calls. Instead, it stores item states in a
    /// thread-safe in-memory store (<see cref="ConcurrentDictionary{TKey, TValue}"/>).
    /// </para>
    /// <para>
    /// Behavior:
    /// <list type="bullet">
    ///   <item><description><see cref="SendCommandAsync(string, OpenHabCommand, CancellationToken)"/> logs the command and sets the item's state to the command's <c>ToString()</c> value.</description></item>
    ///   <item><description><see cref="SendCommandAsync(string, string, CancellationToken)"/> logs the payload and sets the item's state to the raw request body.</description></item>
    ///   <item><description><see cref="GetStateAsync(string, CancellationToken)"/> returns the last stored state or <c>null</c> if none exists.</description></item>
    ///   <item><description><see cref="UpdateStateAsync(string, OpenHabCommand, CancellationToken)"/> sets the item's state to the command's <c>ToString()</c> value.</description></item>
    /// </list>
    /// A small artificial delay is applied to mimic network latency.
    /// </para>
    /// </remarks>
    public sealed class OpenHabClientMock : IOpenHabClient
    {
        private readonly ILogger<OpenHabClientMock> _logger;
        private readonly ConcurrentDictionary<string, string> _states = new(StringComparer.Ordinal);

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenHabClientMock"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public OpenHabClientMock(ILogger<OpenHabClientMock> logger)
        {
            _logger = logger;
        }

        #endregion

        /// <inheritdoc />
        public Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
            => SendCommandAsync(itemname, command.ToString(), ct);

        /// <inheritdoc />
        public async Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must be provided.", nameof(itemname));

            // Simulate a tiny bit of latency to mimic real I/O
            await Task.Delay(25, ct).ConfigureAwait(false);

            _logger.LogInformation("[SIM/openHAB] Command -> Item: {Item}, Body: {Body}", itemname, requestBody);

            // In the mock, sending a command updates the observed state to the command body.
            _states.AddOrUpdate(itemname, requestBody, (_, __) => requestBody);
        }

        /// <inheritdoc />
        public async Task<string?> GetStateAsync(string itemname, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must be provided.", nameof(itemname));

            await Task.Delay(10, ct).ConfigureAwait(false);

            _states.TryGetValue(itemname, out var state);
            _logger.LogDebug("[SIM/openHAB] GetState <- Item: {Item}, State: {State}", itemname, state ?? "<null>");
            return state;
        }

        /// <inheritdoc />
        public async Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must be provided.", nameof(itemname));

            await Task.Delay(25, ct).ConfigureAwait(false);

            var newState = command.ToString();
            _states.AddOrUpdate(itemname, newState, (_, __) => newState);

            _logger.LogInformation("[SIM/openHAB] UpdateState -> Item: {Item}, State: {State}", itemname, newState);
        }
    }
}

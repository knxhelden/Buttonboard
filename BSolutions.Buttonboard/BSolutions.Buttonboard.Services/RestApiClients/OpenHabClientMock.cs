using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// In-memory mock of <see cref="IOpenHabClient"/> for tests and offline development.
    /// Stores item states in memory and simulates small network latencies.
    /// </summary>
    public sealed class OpenHabClientMock : IOpenHabClient
    {
        private readonly ILogger<OpenHabClientMock> _logger;
        private readonly ConcurrentDictionary<string, string> _states =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Creates a new <see cref="OpenHabClientMock"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        public OpenHabClientMock(ILogger<OpenHabClientMock> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
            => SendCommandAsync(itemname, command.ToString(), ct);

        /// <inheritdoc />
        public async Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));
            ct.ThrowIfCancellationRequested();

            // simulate small latency
            await Task.Delay(25, ct).ConfigureAwait(false);

            var body = requestBody ?? string.Empty;
            _states.AddOrUpdate(itemname, body, static (_, __) => body);

            _logger.LogInformation(LogEvents.OpenHabCommandSent,
                "openHAB mock command Item {Item} BodyLength {Length}",
                itemname, body.Length);
        }

        /// <inheritdoc />
        public async Task<string?> GetStateAsync(string itemname, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));
            ct.ThrowIfCancellationRequested();

            await Task.Delay(10, ct).ConfigureAwait(false);

            _states.TryGetValue(itemname, out var state);

            _logger.LogDebug(LogEvents.OpenHabStateRead,
                "openHAB mock state read Item {Item} State {State}",
                itemname, state ?? "<null>");

            return state;
        }

        /// <inheritdoc />
        public async Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));
            ct.ThrowIfCancellationRequested();

            await Task.Delay(25, ct).ConfigureAwait(false);

            var newState = command.ToString();
            _states.AddOrUpdate(itemname, newState, static (_, __) => newState);

            _logger.LogInformation(LogEvents.OpenHabStateUpdated,
                "openHAB mock state updated Item {Item} -> {Command}",
                itemname, command);
        }
    }
}

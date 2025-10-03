using System;
using System.Threading;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// In-memory mock of <see cref="IVlcPlayerClient"/> for tests and offline development.
    /// Simulates command execution with a small delay and structured logging.
    /// </summary>
    public sealed class VlcPlayerClientMock : IVlcPlayerClient
    {
        private readonly ILogger<VlcPlayerClientMock> _logger;

        /// <summary>
        /// Creates a new <see cref="VlcPlayerClientMock"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        public VlcPlayerClientMock(ILogger<VlcPlayerClientMock> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name must be provided.", nameof(playerName));

            ct.ThrowIfCancellationRequested();

            // Simulate tiny I/O latency
            await Task.Delay(15, ct).ConfigureAwait(false);

            _logger.LogInformation(LogEvents.VlcCommandSent,
                "[SIM/VLC] Command {Command} -> Player {Player}",
                command, playerName);
        }
    }
}
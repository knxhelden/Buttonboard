using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// A mock implementation of <see cref="IVlcPlayerClient"/>.
    /// This client does not communicate with a real VLC instance, but instead simulates
    /// command execution. Useful for testing, development, or running the application
    /// without a VLC server available.
    /// </summary>
    public sealed class VlcPlayerClientMock : IVlcPlayerClient
    {
        private readonly ILogger<VlcPlayerClientMock> _logger;

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="VlcPlayerClientMock"/> class.
        /// </summary>
        /// <param name="logger">
        /// The logger used to output diagnostic and simulation information.
        /// </param>
        public VlcPlayerClientMock(ILogger<VlcPlayerClientMock> logger)
        {
            _logger = logger;
        }

        #endregion

        /// <summary>
        /// Simulates sending a command to a VLC player.
        /// No actual network call is made. The command is logged for traceability.
        /// </summary>
        /// <param name="command">The VLC command to simulate sending.</param>
        /// <param name="player">The VLC player definition, required for logging context.</param>
        /// <param name="ct">A cancellation token to cancel the simulation delay.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="player"/> is <c>null</c>.
        /// </exception>
        public async Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player, CancellationToken ct = default)
        {
            if (player is null) throw new ArgumentNullException(nameof(player));

            await Task.Delay(50, ct).ConfigureAwait(false);

            _logger.LogInformation("[SIM] VLC command for player '{Player}': {Command}", player.Name, command);
        }
    }
}

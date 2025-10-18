using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Routes and executes video-related actions such as <c>video.next</c> and <c>video.pause</c>.
    /// </summary>
    /// <remarks>
    /// This router controls remote VLC media players through the legacy HTTP interface
    /// via the <see cref="IVlcPlayerClient"/> abstraction.  
    /// 
    /// Supported operations:
    /// <list type="bullet">
    /// <item><description><c>video.next</c> – Skips to the next item in the current VLC playlist.</description></item>
    /// <item><description><c>video.pause</c> – Pauses or resumes video playback.</description></item>
    /// </list>
    /// </remarks>
    public sealed class VideoActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IVlcPlayerClient _vlc;

        /// <inheritdoc />
        public string Domain => "video";

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoActionRouter"/> class.
        /// </summary>
        /// <param name="logger">The logger used for structured runtime diagnostics.</param>
        /// <param name="settings">The settings provider giving access to VLC device configuration.</param>
        /// <param name="vlc">The VLC client responsible for sending playback commands.</param>
        public VideoActionRouter(
            ILogger<VideoActionRouter> logger,
            ISettingsProvider settings,
            IVlcPlayerClient vlc)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _vlc = vlc ?? throw new ArgumentNullException(nameof(vlc));
        }

        /// <inheritdoc />
        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            switch (op)
            {
                case "next":
                    await HandleNextAsync(step, ct).ConfigureAwait(false);
                    break;

                case "pause":
                    await HandlePauseAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown video action {Action}", key);
                    break;
            }
        }

        /// <summary>
        /// Handles the <c>video.next</c> operation.
        /// Sends a "next" command to the specified VLC player, skipping to the next playlist entry.
        /// </summary>
        /// <param name="step">The scenario step containing the optional <c>player</c> argument.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        /// <exception cref="ArgumentException">Thrown when the specified VLC player is not found.</exception>
        private async Task HandleNextAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var playerName = args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.TryGetValue(playerName, out _))
            {
                _logger.LogWarning(LogEvents.ExecResourceMissing,
                    "video.next: VLC player not found {Player}", playerName);
                throw new ArgumentException($"Unknown VLC player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.next: issuing NEXT to {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the <c>video.pause</c> operation.
        /// Sends a "pause" command to the specified VLC player, toggling playback state.
        /// </summary>
        /// <param name="step">The scenario step containing the optional <c>player</c> argument.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        /// <exception cref="ArgumentException">Thrown when the specified VLC player is not found.</exception>
        private async Task HandlePauseAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var playerName = args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.TryGetValue(playerName, out _))
            {
                _logger.LogWarning(LogEvents.ExecResourceMissing,
                    "video.pause: VLC player not found {Player}", playerName);
                throw new ArgumentException($"Unknown VLC player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecVideoPause,
                "video.pause: issuing PAUSE to {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, playerName, ct).ConfigureAwait(false);
        }
    }
}

using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Routes and executes video-related actions such as <c>video.next</c>, <c>video.pause</c>, and <c>video.playItem</c>.
    /// </summary>
    /// <remarks>
    /// Sends playback commands to VLC players through <see cref="IVlcPlayerClient"/>.
    /// </remarks>
    public sealed class VideoActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IVlcPlayerClient _vlc;

        /// <inheritdoc />
        public string Domain => "video";

        #region --- Constructor ---

        public VideoActionRouter(
            ILogger<VideoActionRouter> logger,
            ISettingsProvider settings,
            IVlcPlayerClient vlc)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _vlc = vlc ?? throw new ArgumentNullException(nameof(vlc));
        }

        #endregion

        #region --- IActionRouter ---

        /// <inheritdoc />
        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            try
            {
                switch (op)
                {
                    case "next":
                        await HandleNextAsync(step, ct);
                        break;
                    case "pause":
                        await HandlePauseAsync(step, ct);
                        break;
                    case "playitem":
                        await HandlePlayItemAsync(step, ct);
                        break;
                    default:
                        _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown video action {Action}", key);
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                // Invalid/missing args etc. → warn and return (no crash)
                _logger.LogWarning(LogEvents.ExecActionArgInvalid, "Video action argument error: {Message}", ex.Message);
            }
            catch (OperationCanceledException)
            {
                // Respect cancellation silently
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected runtime issue → log error and rethrow to bubble up if desired
                _logger.LogError(LogEvents.ExecActionFailed, ex, "Video action failed for {Action}", key);
                throw;
            }
        }

        #endregion

        #region --- Handlers ---

        /// <summary>
        /// Executes <c>video.next</c> — skips to the next playlist entry on the given player.
        /// Required args: <c>player</c>.
        /// </summary>
        private async Task HandleNextAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecVideoNext, "video.next → {Player}", playerName);
            await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct);
        }

        /// <summary>
        /// Executes <c>video.pause</c> — toggles pause/resume on the given player.
        /// Required args: <c>player</c>.
        /// </summary>
        private async Task HandlePauseAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecVideoPause, "video.pause → {Player}", playerName);
            await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, playerName, ct);
        }

        /// <summary>
        /// Executes <c>video.playItem</c> — plays a specific playlist entry at the given position.
        /// Required args: <c>player</c>, <c>position</c> (1-based).
        /// </summary>
        private async Task HandlePlayItemAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            var position = step.Args.GetRequiredInt("position");
            if (position < 1) throw new ArgumentException("position must be >= 1", nameof(position));

            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.playItem → {Player}, position={Position}", playerName, position);

            await _vlc.PlayPlaylistItemAtAsync(playerName, position, ct);
        }

        #endregion

        #region --- Helpers ---

        private void EnsureKnownPlayer(string playerName)
        {
            if (!_settings.VLC.Devices.ContainsKey(playerName))
                throw new ArgumentException($"Unknown VLC player '{playerName}'", nameof(playerName));
        }

        #endregion
    }
}

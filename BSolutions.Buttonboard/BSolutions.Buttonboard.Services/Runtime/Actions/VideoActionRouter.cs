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

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoActionRouter"/> class.
        /// </summary>
        /// <param name="logger">Logger for runtime diagnostics.</param>
        /// <param name="settings">Provides access to VLC player configuration.</param>
        /// <param name="vlc">Client used to send commands to VLC instances.</param>
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
                    await HandleNextAsync(step, ct);
                    break;
                case "pause":
                    await HandlePauseAsync(step, ct);
                    break;
                case "playitem":
                    await HandlePlayItemAsync(step, ct);
                    break;
                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown video action {Action}", key);
                    break;
            }
        }

        /// <summary>
        /// Executes <c>video.next</c> — skips to the next playlist entry on the given player.
        /// </summary>
        /// <param name="step">Scene step containing the optional <c>player</c> argument.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task HandleNextAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var playerName = step.Args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.ContainsKey(playerName))
                throw new ArgumentException($"Unknown VLC player '{playerName}'");

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.next → {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct);
        }

        /// <summary>
        /// Executes <c>video.pause</c> — toggles pause/resume on the given player.
        /// </summary>
        /// <param name="step">Scene step containing the optional <c>player</c> argument.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task HandlePauseAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var playerName = step.Args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.ContainsKey(playerName))
                throw new ArgumentException($"Unknown VLC player '{playerName}'");

            _logger.LogInformation(LogEvents.ExecVideoPause,
                "video.pause → {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, playerName, ct);
        }

        /// <summary>
        /// Executes <c>video.playItem</c> — plays a specific playlist entry at the given position.
        /// </summary>
        /// <param name="step">
        /// Scene step containing:
        /// <list type="bullet">
        /// <item><description><c>player</c> — optional VLC player name (default: <c>Mediaplayer1</c>).</description></item>
        /// <item><description><c>position</c> — 1-based playlist position (default: <c>1</c>).</description></item>
        /// </list>
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        private async Task HandlePlayItemAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var playerName = step.Args.GetString("player", "Mediaplayer1");
            var position = step.Args.GetInt("position", 1);

            if (!_settings.VLC.Devices.ContainsKey(playerName))
                throw new ArgumentException($"Unknown VLC player '{playerName}'");

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.playItem → {Player}, position={Position}", playerName, position);

            await _vlc.PlayPlaylistItemAtAsync(playerName, position, ct);
        }
    }
}

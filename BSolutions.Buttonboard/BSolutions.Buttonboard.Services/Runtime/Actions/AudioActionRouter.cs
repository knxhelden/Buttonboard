using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.LyrionService;
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
    /// Routes and executes audio-related actions such as <c>audio.play</c>, <c>audio.pause</c>, and <c>audio.volume</c>.
    /// </summary>
    /// <remarks>
    /// This router dispatches audio commands to OpenHAB-controlled Squeezebox players.
    /// Supported operations:
    /// <list type="bullet">
    /// <item><description><c>audio.play</c> – Plays an audio file from a given URL on the specified player.</description></item>
    /// <item><description><c>audio.pause</c> – Pauses playback on the specified player.</description></item>
    /// <item><description><c>audio.volume</c> – Adjusts the playback volume of a given player.</description></item>
    /// </list>
    /// The router resolves OpenHAB player configuration from <see cref="ISettingsProvider"/>
    /// and uses <see cref="IOpenHabClient"/> to send commands asynchronously.
    /// </remarks>
    public sealed class AudioActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly ILyrionClient _lyrion;

        /// <inheritdoc />
        public string Domain => "audio";

        #region --- Constructor ---

        public AudioActionRouter(
            ILogger<AudioActionRouter> logger,
            ISettingsProvider settings,
            ILyrionClient lyrion)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _lyrion = lyrion ?? throw new ArgumentNullException(nameof(lyrion));
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
        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            switch (op)
            {
                case "play":
                    await HandlePlayAsync(step, ct).ConfigureAwait(false);
                    break;

                case "pause":
                    await HandlePauseAsync(step, ct).ConfigureAwait(false);
                    break;

                case "volume":
                    await HandleVolumeAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown audio action {Action}", key);
                    break;
            }
        }

        #endregion

        #region --- Handlers ---

        /// <summary>
        /// Handles the <c>audio.play</c> operation.
        /// Sends the provided media URL to the selected OpenHAB audio player.
        /// </summary>
        private async Task HandlePlayAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var url = step.Args.GetString("url");
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing, "audio.play requires argument {Arg}", "url");
                throw new ArgumentException("audio.play requires 'url'");
            }

            var playerName = step.Args.GetString("player", "Player1");
            _logger.LogInformation(LogEvents.ExecAudioPlay, "audio.play via Lyrion: {Player} -> {Url}", playerName, url);
            await _lyrion.PlayUrlAsync(playerName, url, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the <c>audio.pause</c> operation.
        /// Sends a "PAUSE" command to the specified OpenHAB player.
        /// </summary>
        private async Task HandlePauseAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var playerName = step.Args.GetString("player", "Player1");
            var paused = step.Args.GetBool("paused", true); // optional: default = pause
            _logger.LogInformation(LogEvents.ExecAudioPause, "audio.pause via Lyrion: {Player} paused={Paused}", playerName, paused);
            await _lyrion.PauseAsync(playerName, paused, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the <c>audio.volume</c> operation.
        /// Sets the playback volume (0–100%) for the specified OpenHAB player.
        /// </summary>
        private async Task HandleVolumeAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var volume = step.Args.GetInt("volume", -1);
            if (volume < 0 || volume > 100)
            {
                _logger.LogWarning(LogEvents.ExecArgInvalid, "audio.volume requires valid argument {Arg} (0–100)", "volume");
                throw new ArgumentException("audio.volume requires 'volume' between 0 and 100");
            }

            var playerName = step.Args.GetString("player", "Player1");
            _logger.LogInformation(LogEvents.ExecAudioVolume, "audio.volume via Lyrion: {Player} -> {Volume}%", playerName, volume);
            await _lyrion.SetVolumeAsync(playerName, volume, ct).ConfigureAwait(false);
        }

        #endregion
    }
}

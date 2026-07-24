using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Integrations.Lyrion;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Routes and executes LMS-related actions such as <c>lms.play</c>, <c>lms.pause</c>, and <c>lms.volume</c>.
    /// </summary>
    /// <remarks>
    /// Dispatches LMS commands to Lyrion (Logitech Media Server) players via <see cref="ILyrionClient"/>.
    /// Required arguments:
    /// <list type="bullet">
    ///   <item><description><c>lms.play</c> → <c>player</c>, <c>url</c></description></item>
    ///   <item><description><c>lms.pause</c> → <c>player</c> (optional: <c>paused</c>, default: <c>true</c>)</description></item>
    ///   <item><description><c>lms.volume</c> → <c>player</c>, <c>level</c> (0–100)</description></item>
    /// </list>
    /// </remarks>
    public sealed class LmsActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly ILyrionClient _lyrion;

        /// <inheritdoc />
        public string Domain => "lms";

        #region --- Constructor ---

        public LmsActionRouter(
            ILogger<LmsActionRouter> logger,
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
        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            try
            {
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
                        _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown LMS action {Action}", key);
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(LogEvents.ExecActionArgInvalid, "LMS action argument error: {Message}", ex.Message);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.ExecActionFailed, ex, "LMS action failed for {Action}", key);
                throw;
            }
        }

        #endregion

        #region --- Handlers ---

        /// <summary>
        /// Executes <c>lms.play</c> — plays the given URL on the specified player.
        /// Required args: <c>player</c>, <c>url</c>.
        /// </summary>
        private async Task HandlePlayAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            var url = step.Args.GetRequiredString("url");

            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecLmsPlay,
                "lms.play via Lyrion → {Player} -> {Url}", playerName, url);

            await _lyrion.PlayUrlAsync(playerName, url, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes <c>lms.pause</c> — pauses/resumes the specified player.
        /// Required args: <c>player</c>. Optional: <c>paused</c> (default: <c>true</c>).
        /// </summary>
        private async Task HandlePauseAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            var paused = step.Args.GetBool("paused", true);

            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecLmsPause,
                "lms.pause via Lyrion → {Player}, paused={Paused}", playerName, paused);

            await _lyrion.PauseAsync(playerName, paused, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes <c>lms.volume</c> — sets playback volume (0–100) for the specified player.
        /// Required args: <c>player</c>, <c>level</c>.
        /// </summary>
        private async Task HandleVolumeAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var playerName = step.Args.GetRequiredString("player");
            var level = step.Args.GetRequiredInt("level");
            if (level < 0 || level > 100)
                throw new ArgumentException("level must be between 0 and 100", nameof(level));

            EnsureKnownPlayer(playerName);

            _logger.LogInformation(LogEvents.ExecLmsVolume,
                "lms.volume via Lyrion → {Player} -> {Level}%", playerName, level);

            await _lyrion.SetVolumeAsync(playerName, level, ct).ConfigureAwait(false);
        }

        #endregion

        #region --- Helpers ---

        private void EnsureKnownPlayer(string playerName)
        {
            if (!_settings.Lyrion.Players.ContainsKey(playerName))
                throw new ArgumentException($"Unknown Lyrion player '{playerName}'", nameof(playerName));
        }

        #endregion
    }
}

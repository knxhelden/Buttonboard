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
    public sealed class AudioActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IOpenHabClient _openhab;

        public string Domain => "audio";

        public AudioActionRouter(ILogger<AudioActionRouter> logger,
                                 ISettingsProvider settings,
                                 IOpenHabClient openhab)
        {
            _logger = logger;
            _settings = settings;
            _openhab = openhab;
        }

        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            switch (op)
            {
                case "play":
                    await HandlePlayAsync(step, ct).ConfigureAwait(false);
                    break;

                case "volume":
                    await HandleVolumeAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", key);
                    break;
            }
        }

        private async Task HandlePlayAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var url = args.GetString("url");
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing, "audio.play requires argument {Arg}", "url");
                throw new ArgumentException("audio.play requires 'url'");
            }

            var playerName = args.GetString("player", "Player1");

            if (!_settings.OpenHAB.Audio.TryGetValue(playerName, out var player))
            {
                _logger.LogWarning(LogEvents.ExecResourceMissing,
                    "audio.play: OpenHAB player not found {Player}", playerName);
                throw new ArgumentException($"Unknown OpenHAB audio player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecAudioPlay,
                "audio.play: sending URL to player {Player} (Item {StreamItem})",
                playerName, player.StreamItem);

            await _openhab.SendCommandAsync(player.StreamItem, url, ct).ConfigureAwait(false);
        }

        private async Task HandleVolumeAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;

            var volume = args.GetInt("volume", -1);
            if (volume < 0 || volume > 100)
            {
                _logger.LogWarning(LogEvents.ExecArgInvalid,
                    "audio.volume requires valid argument {Arg} (0–100)", "volume");
                throw new ArgumentException("audio.volume requires 'volume' between 0 and 100");
            }

            var playerName = args.GetString("player", "Player1");

            if (!_settings.OpenHAB.Audio.TryGetValue(playerName, out var player))
            {
                _logger.LogWarning(LogEvents.ExecResourceMissing,
                    "audio.volume: OpenHAB player not found {Player}", playerName);
                throw new ArgumentException($"Unknown OpenHAB audio player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecAudioVolume,
                "audio.volume: setting volume of {Player} (Item {VolumeItem}) to {Volume}%",
                playerName, player.VolumeItem, volume);

            await _openhab.SendCommandAsync(player.VolumeItem, volume.ToString(), ct).ConfigureAwait(false);
        }
    }
}

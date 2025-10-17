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
    public sealed class AudioPlayHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IOpenHabClient _openhab;

        public string Key => "audio.play";

        public AudioPlayHandler(ILogger<AudioPlayHandler> logger,
                                ISettingsProvider settings,
                                IOpenHabClient openhab)
        {
            _logger = logger;
            _settings = settings;
            _openhab = openhab;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
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
    }
}

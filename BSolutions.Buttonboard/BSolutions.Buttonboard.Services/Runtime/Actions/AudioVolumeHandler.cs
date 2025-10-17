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
    public sealed class AudioVolumeHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IOpenHabClient _openhab;

        public string Key => "audio.volume";

        public AudioVolumeHandler(ILogger<AudioVolumeHandler> logger,
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

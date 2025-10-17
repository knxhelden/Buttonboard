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
    public sealed class VideoPauseHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IVlcPlayerClient _vlc;

        public string Key => "video.pause";

        public VideoPauseHandler(ILogger<VideoPauseHandler> logger,
                                 ISettingsProvider settings,
                                 IVlcPlayerClient vlc)
        {
            _logger = logger;
            _settings = settings;
            _vlc = vlc;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var playerName = args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Entries.TryGetValue(playerName, out var _))
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

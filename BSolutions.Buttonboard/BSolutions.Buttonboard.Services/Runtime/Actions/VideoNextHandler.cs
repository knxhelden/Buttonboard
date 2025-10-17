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
    public sealed class VideoNextHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IVlcPlayerClient _vlc;

        public string Key => "video.next";

        public VideoNextHandler(ILogger<VideoNextHandler> logger,
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
                    "video.next: VLC player not found {Player}", playerName);
                throw new ArgumentException($"Unknown VLC player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.next: issuing NEXT to {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct).ConfigureAwait(false);
        }
    }
}

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
    public sealed class VideoActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IVlcPlayerClient _vlc;

        public string Domain => "video";

        public VideoActionRouter(ILogger<VideoActionRouter> logger,
                                 ISettingsProvider settings,
                                 IVlcPlayerClient vlc)
        {
            _logger = logger;
            _settings = settings;
            _vlc = vlc;
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
                case "next":
                    await HandleNextAsync(step, ct).ConfigureAwait(false);
                    break;

                case "pause":
                    await HandlePauseAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", key);
                    break;
            }
        }

        private async Task HandleNextAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var playerName = args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.TryGetValue(playerName, out var _))
            {
                _logger.LogWarning(LogEvents.ExecResourceMissing,
                    "video.next: VLC player not found {Player}", playerName);
                throw new ArgumentException($"Unknown VLC player '{playerName}'");
            }

            _logger.LogInformation(LogEvents.ExecVideoNext,
                "video.next: issuing NEXT to {Player}", playerName);

            await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, playerName, ct).ConfigureAwait(false);
        }

        private async Task HandlePauseAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var playerName = args.GetString("player", "Mediaplayer1");

            if (!_settings.VLC.Devices.TryGetValue(playerName, out var _))
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

using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Integrations.Audio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>Routes local Raspberry Pi sound-card actions.</summary>
    public sealed class AudioActionRouter : IActionRouter
    {
        private readonly ILogger<AudioActionRouter> _logger;
        private readonly IAudioPlayer _player;
        private readonly ISettingsProvider _settings;
        public string Domain => "audio";

        public AudioActionRouter(ILogger<AudioActionRouter> logger, IAudioPlayer player, ISettingsProvider settings)
        {
            _logger = logger;
            _player = player;
            _settings = settings;
        }

        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);
            try
            {
                switch (op)
                {
                    case "play":
                        await _player.PlayAsync(
                            step.Args.GetRequiredString("file"),
                            step.Args.GetString("channel", "main"),
                            step.Args.GetInt("volume", _settings.Audio.DefaultVolume),
                            step.Args.GetBool("loop"),
                            ct).ConfigureAwait(false);
                        break;
                    case "stop":
                        await _player.StopAsync(step.Args.GetString("channel", "main"), ct).ConfigureAwait(false);
                        break;
                    case "stopall":
                        await _player.StopAllAsync(ct).ConfigureAwait(false);
                        break;
                    default:
                        _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown audio action {Action}", key);
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(LogEvents.ExecActionArgInvalid, "Audio action argument error: {Message}", ex.Message);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.ExecActionFailed, ex, "Audio action failed for {Action}", key);
                throw;
            }
        }
    }
}

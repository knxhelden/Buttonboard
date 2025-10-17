using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public sealed class GpioActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly IButtonboardGpioController _gpio;

        public string Domain => "gpio";

        public GpioActionRouter(ILogger<GpioActionRouter> logger,
                                IButtonboardGpioController gpio)
        {
            _logger = logger;
            _gpio = gpio;
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
                case "on":
                    await HandleOnAsync(step, ct).ConfigureAwait(false);
                    break;

                case "off":
                    await HandleOffAsync(step, ct).ConfigureAwait(false);
                    break;

                case "blink":
                    await HandleBlinkAsync(step, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", key);
                    break;
            }
        }

        private async Task HandleOnAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var ledStr = step.Args.GetString("led");
            if (string.IsNullOrWhiteSpace(ledStr))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing, "gpio.on requires argument {Arg}", "led");
                throw new ArgumentException("gpio.on requires 'led'");
            }

            var led = ParseLed(ledStr);

            _logger.LogInformation(LogEvents.ExecGpioOn,
                "gpio.on: setting LED {Pin} ON", led);

            await _gpio.LedOnAsync(led, ct).ConfigureAwait(false);
        }

        private async Task HandleOffAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var ledStr = step.Args.GetString("led");
            if (string.IsNullOrWhiteSpace(ledStr))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing, "gpio.off requires argument {Arg}", "led");
                throw new ArgumentException("gpio.off requires 'led'");
            }

            var led = ParseLed(ledStr);

            _logger.LogInformation(LogEvents.ExecGpioOff,
                "gpio.off: setting LED {Pin} OFF", led);

            await _gpio.LedOffAsync(led, ct).ConfigureAwait(false);
        }

        private async Task HandleBlinkAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var count = step.Args.GetInt("count", 3);
            var interval = step.Args.GetInt("intervalMs", 100);

            _logger.LogInformation(LogEvents.ExecGpioBlink,
                "gpio.blink: blinking LEDs Count {Count} IntervalMs {IntervalMs}",
                count, interval);

            await _gpio.LedsBlinkingAsync(count, interval, ct).ConfigureAwait(false);
        }

        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led)) return led;
            throw new ArgumentException($"Unknown Led '{s}'", nameof(s));
        }
    }
}

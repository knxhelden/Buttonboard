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
    public sealed class GpioOnHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly IButtonboardGpioController _gpio;

        public string Key => "gpio.on";

        public GpioOnHandler(ILogger<GpioOnHandler> logger,
                             IButtonboardGpioController gpio)
        {
            _logger = logger;
            _gpio = gpio;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;
            var ledStr = args.GetString("led");
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

        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led)) return led;
            throw new ArgumentException($"Unknown Led '{s}'", nameof(s));
        }
    }
}

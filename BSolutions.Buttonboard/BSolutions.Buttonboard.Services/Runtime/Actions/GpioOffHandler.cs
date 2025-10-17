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
    public sealed class GpioOffHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly IButtonboardGpioController _gpio;

        public string Key => "gpio.off";

        public GpioOffHandler(ILogger<GpioOffHandler> logger,
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
                _logger.LogWarning(LogEvents.ExecArgMissing, "gpio.off requires argument {Arg}", "led");
                throw new ArgumentException("gpio.off requires 'led'");
            }

            var led = ParseLed(ledStr);

            _logger.LogInformation(LogEvents.ExecGpioOff,
                "gpio.off: setting LED {Pin} OFF", led);

            await _gpio.LedOffAsync(led, ct).ConfigureAwait(false);
        }

        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led)) return led;
            throw new ArgumentException($"Unknown Led '{s}'", nameof(s));
        }
    }
}

using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public sealed class GpioBlinkHandler : IActionHandler
    {
        private readonly ILogger _logger;
        private readonly IButtonboardGpioController _gpio;

        public string Key => "gpio.blink";

        public GpioBlinkHandler(ILogger<GpioBlinkHandler> logger,
                                IButtonboardGpioController gpio)
        {
            _logger = logger;
            _gpio = gpio;
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            var args = step.Args;

            var count = args.GetInt("count", 3);
            var interval = args.GetInt("intervalMs", 100);

            _logger.LogInformation(LogEvents.ExecGpioBlink,
                "gpio.blink: blinking LEDs Count {Count} IntervalMs {IntervalMs}",
                count, interval);

            await _gpio.LedsBlinkingAsync(count, interval, ct).ConfigureAwait(false);
        }
    }
}

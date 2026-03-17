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
    /// <summary>
    /// Routes and executes GPIO-related actions such as <c>gpio.on</c>, <c>gpio.off</c>, and <c>gpio.blink</c>.
    /// </summary>
    /// <remarks>
    /// This router controls on-board or external LEDs connected to the Raspberry Pi GPIO pins
    /// via the <see cref="IButtonboardGpioController"/> abstraction.
    /// 
    /// Supported operations:
    /// <list type="bullet">
    /// <item><description><c>gpio.on</c> – Turns a specified LED on.</description></item>
    /// <item><description><c>gpio.off</c> – Turns a specified LED off.</description></item>
    /// <item><description><c>gpio.blink</c> – Performs a blinking sequence on all LEDs.</description></item>
    /// </list>
    /// </remarks>
    public sealed class GpioActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly IButtonboardGpioController _gpio;

        /// <inheritdoc />
        public string Domain => "gpio";

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioActionRouter"/> class.
        /// </summary>
        /// <param name="logger">The logger used for structured runtime diagnostics.</param>
        /// <param name="gpio">The GPIO controller abstraction for LED control.</param>
        public GpioActionRouter(
            ILogger<GpioActionRouter> logger,
            IButtonboardGpioController gpio)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        #endregion

        #region --- IActionRouter ---

        /// <inheritdoc />
        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
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
                    _logger.LogWarning(LogEvents.ExecUnknownAction,
                        "Unknown GPIO action {Action}", key);
                    break;
            }
        }

        #endregion

        #region --- Handlers ---

        /// <summary>
        /// Handles the <c>gpio.on</c> operation by turning on a specific LED.
        /// </summary>
        /// <param name="step">The scenario step containing the <c>led</c> argument.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        private async Task HandleOnAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var ledStr = step.Args.GetString("led");
            if (string.IsNullOrWhiteSpace(ledStr))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing,
                    "gpio.on requires argument {Arg}", "led");
                throw new ArgumentException("gpio.on requires 'led'");
            }

            var led = ParseLed(ledStr);

            _logger.LogInformation(LogEvents.ExecGpioOn,
                "gpio.on: setting LED {Pin} ON", led);

            await _gpio.LedOnAsync(led, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the <c>gpio.off</c> operation by turning off a specific LED.
        /// </summary>
        /// <param name="step">The scenario step containing the <c>led</c> argument.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        private async Task HandleOffAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var ledStr = step.Args.GetString("led");
            if (string.IsNullOrWhiteSpace(ledStr))
            {
                _logger.LogWarning(LogEvents.ExecArgMissing,
                    "gpio.off requires argument {Arg}", "led");
                throw new ArgumentException("gpio.off requires 'led'");
            }

            var led = ParseLed(ledStr);

            _logger.LogInformation(LogEvents.ExecGpioOff,
                "gpio.off: setting LED {Pin} OFF", led);

            await _gpio.LedOffAsync(led, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the <c>gpio.blink</c> operation by blinking all configured LEDs
        /// for a specified number of times and interval.
        /// </summary>
        /// <param name="step">The scenario step containing <c>count</c> and <c>intervalMs</c> arguments.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        private async Task HandleBlinkAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var count = step.Args.GetInt("count", 3);
            var interval = step.Args.GetInt("intervalMs", 100);

            _logger.LogInformation(LogEvents.ExecGpioBlink,
                "gpio.blink: blinking LEDs Count {Count} IntervalMs {IntervalMs}",
                count, interval);

            await _gpio.LedsBlinkingAsync(count, interval, ct).ConfigureAwait(false);
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Parses a string value into a <see cref="Led"/> enum.
        /// </summary>
        /// <param name="s">The LED identifier string (e.g., <c>"Red"</c> or <c>"Led1"</c>).</param>
        /// <returns>The parsed <see cref="Led"/> value.</returns>
        /// <exception cref="ArgumentException">Thrown if the LED name is unknown.</exception>
        private static Led ParseLed(string s)
        {
            if (Enum.TryParse<Led>(s, ignoreCase: true, out var led))
                return led;

            throw new ArgumentException($"Unknown LED '{s}'", nameof(s));
        }

        #endregion


    }
}

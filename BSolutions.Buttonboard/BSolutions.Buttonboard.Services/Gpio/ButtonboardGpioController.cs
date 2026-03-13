using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// Standard GPIO-backed implementation for the physical Buttonboard hardware.
    /// </summary>
    /// <remarks>
    /// This class centralizes pin initialization and board-specific conventions:
    /// buttons are read as active-low inputs with pull-up configuration and LEDs are controlled
    /// through direct pin writes.
    /// </remarks>
    public sealed class ButtonboardGpioController : IButtonboardGpioController, IDisposable
    {
        /// <summary>
        /// Logger used to emit diagnostic and operational GPIO events.
        /// </summary>
        private readonly ILogger<ButtonboardGpioController> _logger;

        /// <summary>
        /// Low-level GPIO controller used for all pin open/read/write operations.
        /// </summary>
        private readonly GpioController _gpio;

        /// <summary>
        /// Creates a new <see cref="ButtonboardGpioController"/>.
        /// </summary>
        /// <param name="settingsProvider">Provides GPIO mapping configuration (reserved for future use).</param>
        /// <param name="gpioController">Underlying GPIO controller.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public ButtonboardGpioController(
            ISettingsProvider settingsProvider,
            GpioController gpioController,
            ILogger<ButtonboardGpioController> logger)
        {
            _ = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _gpio = gpioController ?? throw new ArgumentNullException(nameof(gpioController));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Initialize()
        {
            try
            {
                // Buttons are wired GPIO -> Button -> GND, so use internal pull-up.
                foreach (Button button in Enum.GetValues<Button>())
                {
                    var pin = button.GetGpio();
                    if (!_gpio.IsPinOpen(pin))
                        OpenButtonPin(pin);
                }

                // LEDs → Output (default Low/off)
                foreach (Led led in Enum.GetValues<Led>())
                {
                    var pin = led.GetGpio();
                    if (!_gpio.IsPinOpen(pin))
                        _gpio.OpenPin(pin, PinMode.Output);

                    _gpio.Write(pin, PinValue.Low);
                }

                _logger.LogInformation(LogEvents.GpioInitialized,
                    "GPIO initialized: {Buttons} buttons, {Leds} LEDs",
                    Enum.GetValues<Button>().Length, Enum.GetValues<Led>().Length);

            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex, "GPIO initialization failed");
                throw;
            }
        }

        /// <summary>
        /// Opens one button pin and configures it as input after pull-up setup.
        /// </summary>
        /// <param name="pin">BCM pin number mapped to the button.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when pull-up setup via <c>pinctrl</c> fails.
        /// </exception>
        private void OpenButtonPin(int pin)
        {
            if (!TryConfigurePullUpWithPinctrl(pin))
            {
                throw new InvalidOperationException(
                    $"GPIO pin {pin} pull-up could not be configured via pinctrl. " +
                    "On Raspberry Pi 5 this is required because libgpiod InputPullUp reconfigure can fail with EINVAL.");
            }

            if (_gpio.IsPinOpen(pin))
                _gpio.SetPinMode(pin, PinMode.Input);
            else
                _gpio.OpenPin(pin, PinMode.Input);

            _logger.LogDebug(LogEvents.GpioButtonRead,
                "Button GPIO {Pin} initialized as Input with pull-up configured via pinctrl.", pin);
        }

        /// <summary>
        /// Attempts to enable pull-up mode for a GPIO pin by invoking <c>pinctrl</c>.
        /// </summary>
        /// <param name="pin">BCM pin number to configure.</param>
        /// <returns>
        /// <see langword="true"/> if pull-up was configured successfully; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This path is Linux/Raspberry Pi specific and exists to avoid runtime issues
        /// when switching to <c>InputPullUp</c> on newer Pi platforms.
        /// </remarks>
        private bool TryConfigurePullUpWithPinctrl(int pin)
        {
            if (!OperatingSystem.IsLinux())
                return false;

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "pinctrl",
                    Arguments = $"set {pin} ip pu",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                    return false;

                process.WaitForExit(2000);
                if (!process.HasExited)
                {
                    process.Kill(true);
                    _logger.LogError(LogEvents.GpioOperationErr,
                        "pinctrl timed out while configuring GPIO {Pin} pull-up.", pin);
                    return false;
                }

                if (process.ExitCode == 0)
                    return true;

                var stderr = process.StandardError.ReadToEnd();
                _logger.LogError(LogEvents.GpioOperationErr,
                    "pinctrl failed for GPIO {Pin} with exit code {ExitCode}: {Error}",
                    pin, process.ExitCode, stderr);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex,
                    "pinctrl execution failed while configuring GPIO {Pin} pull-up.", pin);
                return false;
            }
        }

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                foreach (Led led in Enum.GetValues<Led>())
                {
                    _gpio.Write(led.GetGpio(), PinValue.Low);
                }

                _logger.LogInformation(LogEvents.GpioReset, "All LEDs set to OFF");
                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GPIO reset canceled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex, "GPIO reset failed");
                throw;
            }
        }

        /// <inheritdoc />
        public Task LedOnAsync(Led led, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _gpio.Write(led.GetGpio(), PinValue.High);

                _logger.LogInformation(LogEvents.GpioLedOn, "LED set ON {Led}", led);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LED ON canceled {Led}", led);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex, "LED ON failed {Led}", led);
                throw;
            }
        }

        /// <inheritdoc />
        public Task LedOffAsync(Led led, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _gpio.Write(led.GetGpio(), PinValue.Low);

                _logger.LogInformation(LogEvents.GpioLedOff, "LED set OFF {Led}", led);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LED OFF canceled {Led}", led);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex, "LED OFF failed {Led}", led);
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsButtonPressed(Button button)
        {
            try
            {
                var pin = button.GetGpio();
                var pressed = _gpio.Read(pin) == PinValue.Low;
                _logger.LogDebug(LogEvents.GpioButtonRead,
                    "Button read {Button}: {Pressed}", button, pressed);
                return pressed;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex,
                    "Button read failed {Button}", button);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task LedsBlinkingAsync(int repetitions, int intervalMs = 500, CancellationToken ct = default)
        {
            // Use a fixed set of "process" LEDs – adapt if needed.
            Led[] leds =
            {
                Led.ProcessRed1, Led.ProcessRed2, Led.ProcessRed3,
                Led.ProcessYellow1, Led.ProcessYellow2, Led.ProcessYellow3,
                Led.ProcessGreen1, Led.ProcessGreen2, Led.ProcessGreen3
            };

            try
            {
                _logger.LogInformation(LogEvents.GpioBlinkStart,
                    "Blinking LEDs Repetitions {Repetitions} IntervalMs {IntervalMs}",
                    repetitions, intervalMs);

                for (int i = 0; i < repetitions; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    // ON phase
                    foreach (var led in leds)
                        _gpio.Write(led.GetGpio(), PinValue.High);

                    await Task.Delay(intervalMs, ct).ConfigureAwait(false);

                    // OFF phase
                    foreach (var led in leds)
                        _gpio.Write(led.GetGpio(), PinValue.Low);

                    await Task.Delay(intervalMs, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(LogEvents.GpioBlinkEnd,
                    "Blinking completed Repetitions {Repetitions}", repetitions);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Blinking canceled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.GpioOperationErr, ex,
                    "Blinking failed Repetitions {Repetitions} IntervalMs {IntervalMs}",
                    repetitions, intervalMs);
                throw;
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            try
            {
                // LEDs OFF + close
                foreach (Led led in Enum.GetValues<Led>())
                {
                    var pin = led.GetGpio();
                    if (_gpio.IsPinOpen(pin))
                    {
                        _gpio.Write(pin, PinValue.Low);
                        _gpio.ClosePin(pin);
                    }
                }

                // Buttons close
                foreach (Button button in Enum.GetValues<Button>())
                {
                    var pin = button.GetGpio();
                    if (_gpio.IsPinOpen(pin))
                        _gpio.ClosePin(pin);
                }
            }
            catch (Exception ex)
            {
                // Never crash on shutdown; still log for diagnostics.
                _logger.LogWarning(LogEvents.GpioOperationErr, ex, "GPIO dispose encountered errors");
            }
            finally
            {
                _gpio.Dispose();
            }
        }
    }
}

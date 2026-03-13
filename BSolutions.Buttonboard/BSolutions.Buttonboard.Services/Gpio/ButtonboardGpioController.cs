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
    /// Default implementation of <see cref="IButtonboardGpioController"/> based on
    /// <see cref="System.Device.Gpio.GpioController"/> for Raspberry Pi GPIO access.
    /// Initializes pins, provides async LED operations, and synchronous button reads.
    /// </summary>
    public sealed class ButtonboardGpioController : IButtonboardGpioController, IDisposable
    {
        private readonly ILogger<ButtonboardGpioController> _logger;
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

        private void OpenButtonPin(int pin)
        {
            try
            {
                _gpio.OpenPin(pin, PinMode.InputPullUp);

                if (_gpio.GetPinMode(pin) != PinMode.InputPullUp)
                {
                    _logger.LogWarning(LogEvents.GpioOperationErr,
                        "Button GPIO {Pin} did not stay in InputPullUp mode after OpenPin. Trying SetPinMode(InputPullUp).",
                        pin);
                    _gpio.SetPinMode(pin, PinMode.InputPullUp);
                }

                _logger.LogDebug(LogEvents.GpioButtonRead, "Button GPIO {Pin} initialized as InputPullUp", pin);
                return;
            }
            catch (Exception ex)
            {
                // Not every GPIO driver/kernel combination supports internal pull-up via libgpiod.
                // We will try plain input + pinctrl fallback before warning about a floating input.
                _logger.LogDebug(LogEvents.GpioOperationErr, ex,
                    "GPIO pin {Pin} does not support InputPullUp directly. Trying Input mode with pinctrl fallback.", pin);
            }

            try
            {
                if (_gpio.IsPinOpen(pin))
                    _gpio.SetPinMode(pin, PinMode.Input);
                else
                    _gpio.OpenPin(pin, PinMode.Input);

                if (TryConfigurePullUpWithPinctrl(pin))
                {
                    _logger.LogInformation(LogEvents.GpioButtonRead,
                        "Button GPIO {Pin} initialized via pinctrl pull-up fallback (pinctrl set {Pin} ip pu).",
                        pin);
                    return;
                }

                _logger.LogWarning(LogEvents.GpioButtonRead,
                    "GPIO pin {Pin} does not support InputPullUp and pinctrl fallback failed. " +
                    "Button stays in Input mode only; ensure hardware pull-up is present.", pin);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(LogEvents.GpioOperationErr, fallbackEx,
                    "GPIO pin {Pin} could not be initialized in InputPullUp or Input mode.", pin);
                throw;
            }
        }

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
                    _logger.LogWarning(LogEvents.GpioOperationErr,
                        "pinctrl call timed out while configuring GPIO {Pin} pull-up.", pin);
                    return false;
                }

                if (process.ExitCode == 0)
                    return true;

                var stderr = process.StandardError.ReadToEnd();
                _logger.LogDebug(LogEvents.GpioOperationErr,
                    "pinctrl failed for GPIO {Pin} with exit code {ExitCode}: {Error}",
                    pin, process.ExitCode, stderr);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(LogEvents.GpioOperationErr, ex,
                    "pinctrl fallback unavailable for GPIO {Pin}.", pin);
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

        /// <summary>
        /// Closes all pins and disposes the underlying controller.
        /// </summary>
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

using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
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
                // Buttons → Input
                foreach (Button button in Enum.GetValues<Button>())
                {
                    var pin = button.GetGpio();
                    if (!_gpio.IsPinOpen(pin))
                        _gpio.OpenPin(pin, PinMode.Input);
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
                var pressed = _gpio.Read(button.GetGpio()) == PinValue.High;
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
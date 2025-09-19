using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Settings;
using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public class ButtonboardGpioController : IButtonboardGpioController, IDisposable
    {
        private readonly System.Device.Gpio.GpioController _gpio;

        #region --- Constructor ---

        public ButtonboardGpioController(ISettingsProvider settingsProvider,
                              System.Device.Gpio.GpioController gpioController)
        {
            _gpio = gpioController;
        }

        #endregion

        #region --- IGpioController ---

        public void Initialize()
        {
            // Buttons
            foreach (Button button in Enum.GetValues<Button>())
            {
                var pin = button.GetGpio();
                if (!_gpio.IsPinOpen(pin))
                    _gpio.OpenPin(pin, PinMode.Input);
            }

            // LEDs
            foreach (Led led in Enum.GetValues<Led>())
            {
                var pin = led.GetGpio();
                if (!_gpio.IsPinOpen(pin))
                    _gpio.OpenPin(pin, PinMode.Output);
                // Default: aus
                _gpio.Write(pin, PinValue.Low);
            }
        }

        public Task ResetAsync(CancellationToken ct = default)
        {
            // reine IO-Operation → synchron, aber ct respektieren, falls aufgerufen wird, während cancel requested ist
            ct.ThrowIfCancellationRequested();
            foreach (Led led in Enum.GetValues<Led>())
            {
                _gpio.Write(led.GetGpio(), PinValue.Low);
            }
            return Task.CompletedTask;
        }

        public Task LedOnAsync(Led led, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _gpio.Write(led.GetGpio(), PinValue.High);
            return Task.CompletedTask;
        }

        public Task LedOffAsync(Led led, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _gpio.Write(led.GetGpio(), PinValue.Low);
            return Task.CompletedTask;
        }

        public bool IsButtonPressed(Button button)
        {
            return _gpio.Read(button.GetGpio()) == PinValue.High;
        }

        public async Task LedsBlinkingAsync(int repetitions, int intervalMs = 500, CancellationToken ct = default)
        {
            // Einfache Blink-Show für die neun Prozess-LEDs (deine Auswahl beibehalten)
            Led[] leds =
            {
                Led.ProcessRed1, Led.ProcessRed2, Led.ProcessRed3,
                Led.ProcessYellow1, Led.ProcessYellow2, Led.ProcessYellow3,
                Led.ProcessGreen1, Led.ProcessGreen2, Led.ProcessGreen3
            };

            for (int i = 0; i < repetitions; i++)
            {
                ct.ThrowIfCancellationRequested();

                // an
                foreach (var led in leds)
                    _gpio.Write(led.GetGpio(), PinValue.High);

                await Task.Delay(intervalMs, ct).ConfigureAwait(false);

                // aus
                foreach (var led in leds)
                    _gpio.Write(led.GetGpio(), PinValue.Low);

                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
            }
        }

        #endregion

        public void Dispose()
        {
            try
            {
                // Alle Pins sicher aus und schließen
                foreach (Led led in Enum.GetValues<Led>())
                {
                    var pin = led.GetGpio();
                    if (_gpio.IsPinOpen(pin))
                    {
                        _gpio.Write(pin, PinValue.Low);
                        _gpio.ClosePin(pin);
                    }
                }
                foreach (Button button in Enum.GetValues<Button>())
                {
                    var pin = button.GetGpio();
                    if (_gpio.IsPinOpen(pin))
                        _gpio.ClosePin(pin);
                }
            }
            catch
            {
                // Absichtlich schlucken – Dispose darf App nicht crashen
            }
            finally
            {
                _gpio?.Dispose();
            }
        }
    }
}

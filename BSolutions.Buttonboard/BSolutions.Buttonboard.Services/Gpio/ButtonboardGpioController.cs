using BSolutions.Buttonboard.Services.Attributes;
using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Settings;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public class ButtonboardGpioController : IButtonboardGpioController
    {
        private readonly GpioController _gpioController;

        #region --- Constructor ---

        public ButtonboardGpioController(ISettingsProvider settingsProvider, GpioController gpioController)
        {
            this._gpioController = gpioController;
        }

        #endregion

        #region --- IButtonboardGpioController ---

        public void Initialize()
        {
            // Buttons
            foreach (Button button in Enum.GetValues<Button>())
            {
                this._gpioController.OpenPin(button.GetGpio(), PinMode.Input);
            }

            // LEDs
            foreach(Led led in Enum.GetValues<Led>())
            {
                this._gpioController.OpenPin(led.GetGpio(), PinMode.Output);
            }
        }

        public Task ResetAsync()
        {
            return Task.Run(() =>
            {
                // LEDs
                foreach (Led led in Enum.GetValues<Led>())
                {
                    this._gpioController.Write(led.GetGpio(), PinValue.Low);
                }
            });
        }

        public Task LedOnAsync(Led led)
        {
            return Task.Run(() =>
            {
                this._gpioController.Write(led.GetGpio(), PinValue.High);
            });
        }

        public Task LedOffAsync(Led led)
        {
            return Task.Run(() =>
            {
                this._gpioController.Write(led.GetGpio(), PinValue.Low);
            });
        }

        public bool IsButtonPressed(Button button)
        {
            return this._gpioController.Read(button.GetGpio()) == PinValue.High;
        }

        public Task LedsBlinkingAsync(int repetitions, int intervall = 500)
        {
            return Task.Run(() =>
            {
                for (int i = 1; i <= repetitions; i++)
                {
                    this._gpioController.Write(Led.ProcessRed1.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessRed2.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessRed3.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessYellow1.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessYellow2.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessYellow3.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessGreen1.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessGreen2.GetGpio(), PinValue.High);
                    this._gpioController.Write(Led.ProcessGreen3.GetGpio(), PinValue.High);
                    Thread.Sleep(intervall);
                    this._gpioController.Write(Led.ProcessRed1.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessRed2.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessRed3.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessYellow1.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessYellow2.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessYellow3.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessGreen1.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessGreen2.GetGpio(), PinValue.Low);
                    this._gpioController.Write(Led.ProcessGreen3.GetGpio(), PinValue.Low);
                    Thread.Sleep(intervall);
                }
            });
        }

        #endregion

        
    }
}

using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    public abstract class ScenarioBase : IScenario
    {
        #region --- Fields ---

        protected readonly ILogger _logger;
        protected readonly ISettingsProvider _settings;
        protected readonly IOpenHabClient _openhab;
        protected readonly IVlcPlayerClient _vlc;
        protected readonly IButtonboardMqttClient _mqtt;
        protected readonly IButtonboardGpioController _gpioController;

        #endregion

        #region --- Properties ---

        public bool IsScene1Played { get; set; }
        public bool IsScene2Played { get; set; }
        public bool IsScene3Played { get; set; }
        public bool IsScene4Played { get; set; }

        #endregion

        #region --- Constructor ---

        public ScenarioBase(ILogger<ScenarioBase> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpioController,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IButtonboardMqttClient mqtt)
        {
            this._logger = logger;
            this._settings = settingsProvider;
            this._gpioController = gpioController;
            this._openhab = openhab;
            this._vlc = vlc;
            this._mqtt = mqtt;

            this._gpioController.Initialize();
        }

        #endregion

        #region --- IScenario ---

        public async Task RunAsync()
        {
            this._logger.LogInformation($"Szenario wird gestartet ...");
            await this._gpioController.LedOnAsync(Led.SystemGreen);

            try
            {
                while (true)
                {
                    if (this._gpioController.IsButtonPressed(Button.BottomLeft) && this._gpioController.IsButtonPressed(Button.BottomRight))
                    {
                        this._logger.LogInformation("Szenario wird beendet ...");
                        break;
                    }
                    else if (this._gpioController.IsButtonPressed(Button.TopCenter))
                    {
                        await this.RunScene1();
                    }
                    else if (this._gpioController.IsButtonPressed(Button.BottomLeft))
                    {
                        await this.RunScene2();
                    }
                    else if (this._gpioController.IsButtonPressed(Button.BottomCenter))
                    {
                        await this.RunScene3();
                    }
                    else if (this._gpioController.IsButtonPressed(Button.BottomRight))
                    {
                        await this.RunScene4();
                    }

                    Thread.Sleep(180);
                }
            }
            catch (Exception ex)
            {
                await this._gpioController.LedOnAsync(Led.SystemYellow);
                Console.WriteLine($"Es ist ein Fehler aufgetreten: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Drücke eine beliebige Taste, um die Applikation zu beenden ...");
                Console.WriteLine();
                Console.ReadKey();
            }
        }

        public virtual async Task SetupAsync()
        {
            this._logger.LogInformation($"Szenario wird eingerichtet ...");

            // Button Top Center Led
            await this._gpioController.LedOnAsync(Led.ButtonTopCenter);
        }

        public virtual async Task ResetAsync()
        {
            this._logger.LogInformation($"Szenario wird zurückgesetzt ...");

            // GPIO
            await this._gpioController.ResetAsync();

            // History
            this.IsScene1Played = false;
            this.IsScene2Played = false;
            this.IsScene3Played = false;
            this.IsScene4Played = false;
        }

        #endregion

        #region --- Basic Scene Methods ---

        protected virtual Task RunScene1()
        {
            return Task.Run(() =>
            {
                this._logger.LogInformation("Szene 1 wurde gestartet ...");

                // Process LEDs
                this._gpioController.LedOffAsync(Led.ProcessRed1);
                this._gpioController.LedOffAsync(Led.ProcessRed2);
                this._gpioController.LedOffAsync(Led.ProcessRed3);
                this._gpioController.LedOffAsync(Led.ProcessYellow1);
                this._gpioController.LedOffAsync(Led.ProcessYellow2);
                this._gpioController.LedOffAsync(Led.ProcessYellow3);
                this._gpioController.LedOffAsync(Led.ProcessGreen1);
                this._gpioController.LedOffAsync(Led.ProcessGreen2);
                this._gpioController.LedOffAsync(Led.ProcessGreen3);

                // Button LED
                this._gpioController.LedOnAsync(Led.ButtonTopCenter);

                // History
                this.IsScene1Played = true;
            });
        }

        protected virtual Task RunScene2()
        {
            return Task.Run(() =>
            {
                if(this.IsScene1Played == false)
                {
                    this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 2 wurde gestartet ...");

                    this._gpioController.LedOnAsync(Led.ProcessRed1);
                    this._gpioController.LedOnAsync(Led.ProcessRed2);
                    this._gpioController.LedOnAsync(Led.ProcessRed3);

                    // Button LED
                    this._gpioController.LedOnAsync(Led.ButtonBottomLeft);

                    // History
                    this.IsScene2Played = true;
                }
            });
        }

        protected virtual Task RunScene3()
        {
            return Task.Run(() =>
            {
                if (this.IsScene2Played == false)
                {
                    this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 3 wurde gestartet ...");

                    this._gpioController.LedOnAsync(Led.ProcessYellow1);
                    this._gpioController.LedOnAsync(Led.ProcessYellow2);
                    this._gpioController.LedOnAsync(Led.ProcessYellow3);

                    // Button LED
                    this._gpioController.LedOnAsync(Led.ButtonBottomCenter);

                    // History
                    this.IsScene3Played = true;
                }
            });
        }

        protected virtual Task RunScene4()
        {
            return Task.Run(() =>
            {
                if (this.IsScene3Played == false)
                {
                    this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 4 wurde gestartet ...");

                    this._gpioController.LedOnAsync(Led.ProcessGreen1);
                    this._gpioController.LedOnAsync(Led.ProcessGreen2);
                    this._gpioController.LedOnAsync(Led.ProcessGreen3);

                    // Button LED
                    this._gpioController.LedOnAsync(Led.ButtonBottomRight);

                    // History
                    this.IsScene4Played = true;
                }
            });
        }

        #endregion
    }
}

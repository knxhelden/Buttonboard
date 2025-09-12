using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
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
            _logger = logger;
            _settings = settingsProvider;
            _gpioController = gpioController;
            _openhab = openhab;
            _vlc = vlc;
            _mqtt = mqtt;

            _gpioController.Initialize();
        }

        #endregion

        #region --- IScenario ---

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation($"Scenario is running…");
            await _gpioController.LedOnAsync(Led.SystemGreen);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_gpioController.IsButtonPressed(Button.BottomLeft) && _gpioController.IsButtonPressed(Button.BottomRight))
                    {
                        _logger.LogInformation("Scenario is terminated…");
                        break;
                    }
                    else if (_gpioController.IsButtonPressed(Button.TopCenter))
                    {
                        await RunScene1(ct);
                    }
                    else if (_gpioController.IsButtonPressed(Button.BottomLeft))
                    {
                        await RunScene2(ct);
                    }
                    else if (_gpioController.IsButtonPressed(Button.BottomCenter))
                    {
                        await RunScene3(ct);
                    }
                    else if (_gpioController.IsButtonPressed(Button.BottomRight))
                    {
                        await RunScene4(ct);
                    }

                    await Task.Delay(180, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Cooperative shutdown: expected during Ctrl+C or service stop
                _logger.LogInformation("Scenario cancellation requested. Shutting down gracefully…");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scenario has an unexpected error ScenarioBase.RunAsync.");
            }

            _logger.LogInformation($"Scenario has ended.");
        }

        public virtual async Task SetupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation($"Scenario is being set up…");

            // Button Top Center Led
            await _gpioController.LedOnAsync(Led.ButtonTopCenter);

            _logger.LogInformation($"Scenario has been set up.");
        }

        public virtual async Task ResetAsync(CancellationToken ct = default)
        {
            _logger.LogInformation($"Scenario is being reset…");

            // GPIO
            await _gpioController.ResetAsync();

            // History
            IsScene1Played = false;
            IsScene2Played = false;
            IsScene3Played = false;
            IsScene4Played = false;

            _logger.LogInformation($"Scenario has been reset.");
        }

        #endregion

        #region --- Basic Scene Methods ---

        protected virtual async Task RunScene1(CancellationToken ct = default)
        {
            _logger.LogInformation("Scene 1 has started…");

            // Process LEDs
            await _gpioController.LedOffAsync(Led.ProcessRed1);
            await _gpioController.LedOffAsync(Led.ProcessRed2);
            await _gpioController.LedOffAsync(Led.ProcessRed3);
            await _gpioController.LedOffAsync(Led.ProcessYellow1);
            await _gpioController.LedOffAsync(Led.ProcessYellow2);
            await _gpioController.LedOffAsync(Led.ProcessYellow3);
            await _gpioController.LedOffAsync(Led.ProcessGreen1);
            await _gpioController.LedOffAsync(Led.ProcessGreen2);
            await _gpioController.LedOffAsync(Led.ProcessGreen3);

            // Button LED
            await _gpioController.LedOnAsync(Led.ButtonTopCenter);

            // History
            IsScene1Played = true;

            _logger.LogInformation("Scene 1 has ended.");
        }

        protected virtual async Task RunScene2(CancellationToken ct = default)
        {
            if (IsScene1Played == false)
            {
                await _gpioController.LedsBlinkingAsync(5, 100);
            }
            else
            {
                _logger.LogInformation("Scene 2 has started…");

                await _gpioController.LedOnAsync(Led.ProcessRed1);
                await _gpioController.LedOnAsync(Led.ProcessRed2);
                await _gpioController.LedOnAsync(Led.ProcessRed3);

                // Button LED
                await _gpioController.LedOnAsync(Led.ButtonBottomLeft);

                // History
                IsScene2Played = true;

                _logger.LogInformation("Scene 2 has ended.");
            }
        }

        protected virtual async Task RunScene3(CancellationToken ct = default)
        {
            if (IsScene2Played == false)
            {
                await _gpioController.LedsBlinkingAsync(5, 100);
            }
            else
            {
                _logger.LogInformation("Scene 3 has started…");

                await _gpioController.LedOnAsync(Led.ProcessYellow1);
                await _gpioController.LedOnAsync(Led.ProcessYellow2);
                await _gpioController.LedOnAsync(Led.ProcessYellow3);

                // Button LED
                await _gpioController.LedOnAsync(Led.ButtonBottomCenter);

                // History
                IsScene3Played = true;

                _logger.LogInformation("Scene 3 has ended.");
            }
        }

        protected virtual async Task RunScene4(CancellationToken ct = default)
        {
            if (IsScene3Played == false)
            {
                await _gpioController.LedsBlinkingAsync(5, 100);
            }
            else
            {
                _logger.LogInformation("Scene 4 has started…");

                await _gpioController.LedOnAsync(Led.ProcessGreen1);
                await _gpioController.LedOnAsync(Led.ProcessGreen2);
                await _gpioController.LedOnAsync(Led.ProcessGreen3);

                // Button LED
                await _gpioController.LedOnAsync(Led.ButtonBottomRight);

                // History
                IsScene4Played = true;

                _logger.LogInformation("Scene 4 has ended.");
            }
        }

        #endregion
    }
}

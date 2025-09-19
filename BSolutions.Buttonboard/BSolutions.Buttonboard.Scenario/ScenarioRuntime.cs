using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Runtimes;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    public class ScenarioRuntime : IScenario
    {
        #region --- Fields ---

        protected readonly ILogger _logger;
        protected readonly ISettingsProvider _settings;
        private readonly ISceneRuntime _sceneRuntime;
        private readonly IButtonboardGpioController _gpio;

        private const string Scene1 = "scene1";
        private const string Scene2 = "scene2";
        private const string Scene3 = "scene3";
        private const string Scene4 = "scene4";

        private readonly Dictionary<Button, bool> _lastState = new();
        private readonly Dictionary<Button, long> _lastFireMs = new();
        private const int DebounceMs = 150;

        #endregion

        #region --- Properties ---

        public bool IsScene1Played { get; set; }
        public bool IsScene2Played { get; set; }
        public bool IsScene3Played { get; set; }
        public bool IsScene4Played { get; set; }

        #endregion

        #region --- Constructor ---

        public ScenarioRuntime(ILogger<ScenarioRuntime> logger,
            ISettingsProvider settingsProvider,
            ISceneRuntime sceneRuntime,
            IButtonboardGpioController gpio)
        {
            _logger = logger;
            _settings = settingsProvider;
            _sceneRuntime = sceneRuntime;
            _gpio = gpio;

            foreach (var b in Enum.GetValues<Button>())
            {
                _lastState[b] = false;
                _lastFireMs[b] = -1;
            }
        }

        #endregion

        #region --- IScenario ---

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is running…");
            await _gpio.LedOnAsync(Led.SystemGreen);

            var sw = Stopwatch.StartNew();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Termination combo
                    if (_gpio.IsButtonPressed(Button.BottomLeft) && _gpio.IsButtonPressed(Button.BottomRight))
                    {
                        _logger.LogInformation("Termination combo detected → cancel current scene.");
                        await _sceneRuntime.CancelAsync();
                        break;
                    }

                    // Buttons mit Rising-Edge + Entprellung
                    HandleButtonRisingEdge(sw, Button.TopCenter, () => RunScene1(ct));
                    HandleButtonRisingEdge(sw, Button.BottomLeft, () => RunScene2(ct));
                    HandleButtonRisingEdge(sw, Button.BottomCenter, () => RunScene3(ct));
                    HandleButtonRisingEdge(sw, Button.BottomRight, () => RunScene4(ct));

                    await Task.Delay(20, ct); // feineres Polling mit Debounce, statt 180ms-Chunk
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scenario cancellation requested. Shutting down gracefully…");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scenario encountered an unexpected error in ScenarioBase.RunAsync.");
            }

            _logger.LogInformation("Scenario has ended.");
        }

        public virtual async Task SetupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation($"Scenario is being set up…");

            // Button Top Center Led
            await _gpio.LedOnAsync(Led.ButtonTopCenter);

            _logger.LogInformation($"Scenario has been set up.");
        }

        public virtual async Task ResetAsync(CancellationToken ct = default)
        {
            _logger.LogInformation($"Scenario is being reset…");

            await _sceneRuntime.CancelAsync();
            await _gpio.ResetAsync();

            // History
            IsScene1Played = false;
            IsScene2Played = false;
            IsScene3Played = false;
            IsScene4Played = false;

            _logger.LogInformation($"Scenario has been reset.");
        }

        #endregion

        private void HandleButtonRisingEdge(Stopwatch sw, Button button, Func<Task> onPressedAsync)
        {
            var pressed = _gpio.IsButtonPressed(button);
            var wasPressed = _lastState[button];
            _lastState[button] = pressed;

            if (!pressed || wasPressed) return; // nur Rising-Edge

            var now = sw.ElapsedMilliseconds;
            var last = _lastFireMs[button];
            if (last >= 0 && (now - last) < DebounceMs) return; // entprellt

            _lastFireMs[button] = now;

            // Fire & forget, die Methode selbst loggt/handhabt Fehler
            _ = onPressedAsync();
        }

        #region --- Basic Scene Methods ---

        protected virtual async Task RunScene1(CancellationToken ct = default)
        {
            _logger.LogInformation("Scene 1 triggered…");

            var started = await _sceneRuntime.StartAsync(Scene1, ct);
            if (!started)
            {
                _logger.LogInformation("Scene 1 ignored because another scene is running.");
                return;
            }

            IsScene1Played = true;
        }

        protected virtual async Task RunScene2(CancellationToken ct = default)
        {
            if (!IsScene1Played)
            {
                await _gpio.LedsBlinkingAsync(5, 100);
                return;
            }

            _logger.LogInformation("Scene 2 triggered…");

            var started = await _sceneRuntime.StartAsync(Scene2, ct);
            if (!started)
            {
                _logger.LogInformation("Scene 2 ignored because another scene is running.");
                return;
            }

            IsScene2Played = true;
        }

        protected virtual async Task RunScene3(CancellationToken ct = default)
        {
            if (!IsScene2Played)
            {
                await _gpio.LedsBlinkingAsync(5, 100);
                return;
            }

            _logger.LogInformation("Scene 3 triggered…");

            var started = await _sceneRuntime.StartAsync(Scene3, ct);
            if (!started)
            {
                _logger.LogInformation("Scene 3 ignored because another scene is running.");
                return;
            }

            IsScene3Played = true;
        }

        protected virtual async Task RunScene4(CancellationToken ct = default)
        {
            if (!IsScene3Played)
            {
                await _gpio.LedsBlinkingAsync(5, 100);
                return;
            }

            _logger.LogInformation("Scene 4 triggered…");

            var started = await _sceneRuntime.StartAsync(Scene4, ct);
            if (!started)
            {
                _logger.LogInformation("Scene 4 ignored because another scene is running.");
                return;
            }

            IsScene4Played = true;
        }

        #endregion
    }
}

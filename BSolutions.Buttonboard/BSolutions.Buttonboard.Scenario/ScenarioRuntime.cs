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
    /// <summary>
    /// Orchestrates the execution of JSON-based scenes triggered by hardware buttons.
    /// 
    /// Features:
    /// - Rising-edge detection with debouncing
    /// - Stage-based progression of scenes (1→2→3→4, cyclic)
    /// - Termination combo to cancel the current scene
    /// - TestOperation mode (from <see cref="Application.TestOperation"/>):
    ///   ignores stage progression and allows any button to trigger its scene at any time
    /// </summary>
    public sealed class ScenarioRuntime : IScenario
    {
        #region --- Fields ---

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IScenarioAssetRuntime _sceneRuntime;
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Cached flag from settings: if true, stage order is ignored and any button may trigger its scene.
        /// </summary>
        private readonly bool _disableSceneOrder;

        /// <summary>
        /// Debounce window in milliseconds to suppress multiple triggers caused by mechanical button bounce.
        /// </summary>
        private const int DebounceMs = 150;

        /// <summary>
        /// Poll delay for the main loop in milliseconds.
        /// Lower values = more responsive (but more CPU).
        /// Higher values = less CPU (but less responsive).
        /// </summary>
        private const int PollDelayMs = 20;

        /// <summary>
        /// Current progression stage (0..=_scenes.Count - 1).
        /// In normal mode this enforces sequential playback.
        /// </summary>
        private int _stage = 0;

        /// <summary>
        /// Last observed pressed-state per button (for rising-edge detection).
        /// </summary>
        private readonly Dictionary<Button, bool> _lastState = new();

        /// <summary>
        /// Timestamp (ms since scenario start) of the last accepted press per button (for debouncing).
        /// </summary>
        private readonly Dictionary<Button, long> _lastFireMs = new();

        /// <summary>
        /// Data-driven list of scenes (order defines progression in normal mode).
        /// </summary>
        private readonly List<SceneDef> _scenes;

        #endregion

        #region --- Scene Model ---

        /// <summary>
        /// Compact record type describing a scene definition.
        /// </summary>
        private readonly record struct SceneDef(string Key, Button TriggerButton, int RequiredStage);

        #endregion

        #region --- Properties ---

        /// <summary>True if scene 1 has been unlocked or passed in the current cycle.</summary>
        public bool IsScene1Played => _stage >= 1;

        /// <summary>True if scene 2 has been unlocked or passed in the current cycle.</summary>
        public bool IsScene2Played => _stage >= 2;

        /// <summary>True if scene 3 has been unlocked or passed in the current cycle.</summary>
        public bool IsScene3Played => _stage >= 3;

        /// <summary>True if scene 4 has been unlocked or passed in the current cycle.</summary>
        public bool IsScene4Played => _stage >= 4;

        #endregion

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="ScenarioRuntime"/>.
        /// </summary>
        /// <param name="logger">Logger for runtime diagnostics.</param>
        /// <param name="settings">Provides access to application settings.</param>
        /// <param name="sceneRuntime">Service to start/cancel scenes.</param>
        /// <param name="gpio">GPIO controller to read buttons and control LEDs.</param>
        public ScenarioRuntime(
            ILogger<ScenarioRuntime> logger,
            ISettingsProvider settings,
            IScenarioAssetRuntime sceneRuntime,
            IButtonboardGpioController gpio)
        {
            _logger = logger;
            _settings = settings;
            _sceneRuntime = sceneRuntime;
            _gpio = gpio;

            // Cache flag once at startup (hot-reload not required here).
            _disableSceneOrder = _settings.Application.DisableSceneOrder;

            // Define available scenes (button ↔ key mapping).
            _scenes = new()
            {
                new SceneDef("scene1", Button.TopCenter,    0),
                new SceneDef("scene2", Button.BottomLeft,   1),
                new SceneDef("scene3", Button.BottomCenter, 2),
                new SceneDef("scene4", Button.BottomRight,  3),
            };

            // Initialize edge/debounce tracking.
            foreach (var b in Enum.GetValues<Button>())
            {
                _lastState[b] = false;
                _lastFireMs[b] = -1;
            }
        }

        #endregion

        #region --- IScenario ---

        /// <summary>
        /// Runs the scenario loop until cancelled:
        /// - Polls buttons in a tight loop
        /// - Detects termination combo (BottomLeft+BottomRight)
        /// - Invokes scenes on valid button presses
        /// </summary>
        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is running… (TestOperation={Test})", _disableSceneOrder);
            await _gpio.LedOnAsync(Led.SystemGreen);

            var sw = Stopwatch.StartNew();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Termination combo → cancel current scene and exit.
                    if (_gpio.IsButtonPressed(Button.BottomLeft) && _gpio.IsButtonPressed(Button.BottomRight))
                    {
                        _logger.LogInformation("Termination combo detected → cancel current scene.");
                        await _sceneRuntime.CancelAsync();
                        break;
                    }

                    // Poll all scene triggers.
                    foreach (var s in _scenes)
                    {
                        HandleButtonRisingEdge(sw, s.TriggerButton, () => TryTriggerSceneAsync(s, ct));
                    }

                    await Task.Delay(PollDelayMs, ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scenario cancellation requested. Shutting down gracefully…");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scenario encountered an unexpected error in RunAsync.");
            }

            _logger.LogInformation("Scenario has ended.");
        }

        /// <summary>
        /// Prepares the scenario for operation.
        /// Attempts to start the optional "setup" scene (Setup.json).
        /// </summary>
        public async Task SetupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being set up…");

            var started = await _sceneRuntime.StartAsync("setup", ct);
            if (!started)
            {
                _logger.LogWarning("Setup scene not started (missing, busy, or error).");
            }
        }

        /// <summary>
        /// Resets to a clean state:
        /// - Cancels any running scene
        /// - Resets GPIO
        /// - Resets progression stage
        /// </summary>
        public async Task ResetAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being reset…");

            await _sceneRuntime.CancelAsync();
            await _gpio.ResetAsync();

            _stage = 0;
        }

        #endregion

        #region --- Private Helpers ---

        /// <summary>
        /// Detects a rising edge (not pressed → pressed) with debounce.
        /// Invokes the supplied asynchronous action fire-and-forget.
        /// </summary>
        private void HandleButtonRisingEdge(Stopwatch sw, Button button, Func<Task> onPressedAsync)
        {
            var pressed = _gpio.IsButtonPressed(button);
            var wasPressed = _lastState[button];
            _lastState[button] = pressed;

            if (!pressed || wasPressed) return;

            var now = sw.ElapsedMilliseconds;
            var last = _lastFireMs[button];

            if (last >= 0 && (now - last) < DebounceMs) return;

            _lastFireMs[button] = now;

            _ = SafeFireAndForget(onPressedAsync);
        }

        /// <summary>
        /// Runs an asynchronous action and swallows exceptions.
        /// Errors must be logged inside the action itself.
        /// </summary>
        private static async Task SafeFireAndForget(Func<Task> action)
        {
            try { await action().ConfigureAwait(false); }
            catch { /* intentionally ignored */ }
        }

        /// <summary>
        /// Attempts to trigger a scene:
        /// - In TestOperation mode: always starts the scene, regardless of stage
        /// - In normal mode: enforces sequential stage progression
        /// </summary>
        private async Task TryTriggerSceneAsync(SceneDef scene, CancellationToken ct)
        {
            if (_disableSceneOrder)
            {
                _logger.LogInformation("TestOperation active → starting scene {Key} regardless of stage.", scene.Key);

                var startedTest = await _sceneRuntime.StartAsync(scene.Key, ct);
                if (!startedTest)
                {
                    _logger.LogInformation("Scene {Key} ignored because another scene is running.", scene.Key);
                }
                return;
            }

            // Normal mode checks.
            if (_stage < scene.RequiredStage || _stage > scene.RequiredStage)
            {
                await _gpio.LedsBlinkingAsync(5, 100);
                return;
            }

            _logger.LogInformation("Scene {Key} triggered…", scene.Key);

            var started = await _sceneRuntime.StartAsync(scene.Key, ct);
            if (!started)
            {
                _logger.LogInformation("Scene {Key} ignored because another scene is running.", scene.Key);
                return;
            }

            _stage = (scene.RequiredStage + 1) % _scenes.Count;
        }

        #endregion
    }
}

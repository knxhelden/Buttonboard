using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Runtimes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    /// <summary>
    /// Orchestrates a sequence of scenes that are triggered by hardware buttons.
    /// Implements a stage-based state machine with debounced, rising-edge button handling.
    /// Scenes are defined data-driven (key + trigger button + required stage) and run in a loop.
    /// </summary>
    public sealed class ScenarioRuntime : IScenario
    {
        #region --- Fields ---

        private readonly ILogger _logger;
        private readonly ISceneRuntime _sceneRuntime;
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Debounce window in milliseconds to suppress multiple triggers caused by mechanical button bounce.
        /// </summary>
        private const int DebounceMs = 150;

        /// <summary>
        /// Poll delay for the main loop. Lower values = more responsive (but more CPU); higher values = less CPU (but less responsive).
        /// </summary>
        private const int PollDelayMs = 20;

        /// <summary>
        /// Current progression stage (0..=_scenes.Count - 1 for the next allowed scene).
        /// After a scene successfully starts, this value advances cyclically.
        /// </summary>
        private int _stage = 0;

        /// <summary>
        /// Last observed pressed-state per button (used to detect rising edges).
        /// </summary>
        private readonly Dictionary<Button, bool> _lastState = new();

        /// <summary>
        /// Timestamp (ms since scenario start) of the last accepted trigger per button (used for debouncing).
        /// </summary>
        private readonly Dictionary<Button, long> _lastFireMs = new();

        /// <summary>
        /// Data-driven scene list (order defines progression).
        /// </summary>
        private readonly List<SceneDef> _scenes;

        #endregion

        #region --- Scene Model ---

        /// <summary>
        /// Compact value type describing a scene definition.
        /// </summary>
        private readonly record struct SceneDef(string Key, Button TriggerButton, int RequiredStage);

        #endregion

        #region --- Properties ---

        /// <summary>
        /// Indicates whether scene 1 is unlocked or already passed in the current cycle.
        /// Note: This reflects current progression, not historical playback.
        /// </summary>
        public bool IsScene1Played => _stage >= 1;

        /// <summary>
        /// Indicates whether scene 2 is unlocked or already passed in the current cycle.
        /// </summary>
        public bool IsScene2Played => _stage >= 2;

        /// <summary>
        /// Indicates whether scene 3 is unlocked or already passed in the current cycle.
        /// </summary>
        public bool IsScene3Played => _stage >= 3;

        /// <summary>
        /// Indicates whether scene 4 is unlocked or already passed in the current cycle.
        /// </summary>
        public bool IsScene4Played => _stage >= 4;

        #endregion

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of <see cref="ScenarioRuntime"/>.
        /// </summary>
        /// <param name="logger">Logger for runtime diagnostics.</param>
        /// <param name="sceneRuntime">Abstraction that starts/cancels scenes.</param>
        /// <param name="gpio">GPIO controller to read buttons and control LEDs.</param>
        public ScenarioRuntime(
            ILogger<ScenarioRuntime> logger,
            ISceneRuntime sceneRuntime,
            IButtonboardGpioController gpio)
        {
            _logger = logger;
            _sceneRuntime = sceneRuntime;
            _gpio = gpio;

            // Data-driven definition (RequiredStage = stage that must be met BEFORE the scene may start).
            // Adjust this list to change order, buttons, or add/remove scenes.
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
        /// Runs the scenario loop: polls buttons, detects termination combo, and triggers scenes on rising edges.
        /// </summary>
        /// <param name="ct">Cancellation token used to stop the loop gracefully.</param>
        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is running…");
            await _gpio.LedOnAsync(Led.SystemGreen); // Provide a visible "running" indicator.

            var sw = Stopwatch.StartNew(); // Monotonic time for debouncing.

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Termination combo: pressing BottomLeft + BottomRight cancels the current scene.
                    // Keep this check simple and fast, as it executes each poll iteration.
                    if (_gpio.IsButtonPressed(Button.BottomLeft) && _gpio.IsButtonPressed(Button.BottomRight))
                    {
                        _logger.LogInformation("Termination combo detected → cancel current scene.");
                        await _sceneRuntime.CancelAsync();
                        break;
                    }

                    // Check all scenes for a rising-edge on their trigger buttons.
                    foreach (var s in _scenes)
                    {
                        HandleButtonRisingEdge(sw, s.TriggerButton, () => TryTriggerSceneAsync(s, ct));
                    }

                    await Task.Delay(PollDelayMs, ct); // Tuning knob: latency vs. CPU usage.
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
        /// Prepares the scenario for operation (e.g., sets initial LED state).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task SetupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being set up…");

            // Versuche die Setup-Szene (Setup.json) zu starten.
            // StartAsync gibt false zurück, wenn bereits eine Szene läuft
            // oder wenn "setup" nicht gefunden werden konnte.
            var started = await _sceneRuntime.StartAsync("setup", ct);
            if (!started)
            {
                _logger.LogWarning("Setup scene not started (missing, busy, or error).");
            }

            _logger.LogInformation("Scenario has been set up.");
        }

        /// <summary>
        /// Resets the scenario to a clean state: cancels any running scene, resets GPIO, and resets the progression.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task ResetAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being reset…");

            await _sceneRuntime.CancelAsync();
            await _gpio.ResetAsync();

            _stage = 0; // Next allowed scene is the first scene again.

            _logger.LogInformation("Scenario has been reset.");
        }

        #endregion

        #region --- Private Helpers ---

        /// <summary>
        /// Detects a button <b>rising edge</b> (transition from not pressed → pressed) with debouncing
        /// and invokes the supplied asynchronous action (fire-and-forget).
        /// </summary>
        /// <param name="sw">Stopwatch used as a monotonic time source for debouncing.</param>
        /// <param name="button">Button to inspect.</param>
        /// <param name="onPressedAsync">Action to invoke once on a valid press.</param>
        private void HandleButtonRisingEdge(Stopwatch sw, Button button, Func<Task> onPressedAsync)
        {
            var pressed = _gpio.IsButtonPressed(button);
            var wasPressed = _lastState[button];
            _lastState[button] = pressed;

            // Rising edge: only react when the button just transitioned to "pressed".
            if (!pressed || wasPressed) return;

            var now = sw.ElapsedMilliseconds;
            var last = _lastFireMs[button];

            // Debounce: ignore triggers occurring too soon after the last accepted press.
            if (last >= 0 && (now - last) < DebounceMs) return;

            _lastFireMs[button] = now;

            // Fire-and-forget: we deliberately do not await here to keep the poll loop snappy.
            // Any exception handling/logging must happen inside the invoked task.
            _ = SafeFireAndForget(onPressedAsync);
        }

        /// <summary>
        /// Runs an asynchronous action and swallows exceptions to avoid surfacing unobserved task exceptions.
        /// The action itself is expected to log errors as needed.
        /// </summary>
        /// <param name="action">The asynchronous action to run.</param>
        private static async Task SafeFireAndForget(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
                // Intentionally ignored. The inner action should take care of logging.
            }
        }

        /// <summary>
        /// Attempts to trigger a specific scene based on current stage and scene definition.
        /// Provides LED feedback if the scene is not yet eligible or has already been passed in this cycle.
        /// </summary>
        /// <param name="scene">Scene definition (key, trigger button, required stage).</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task TryTriggerSceneAsync(SceneDef scene, CancellationToken ct)
        {
            // Not yet eligible: user pressed a later scene too early → show feedback.
            if (_stage < scene.RequiredStage)
            {
                await _gpio.LedsBlinkingAsync(5, 100);
                return;
            }

            // Already passed in this cycle: keep idempotency and give feedback (optional).
            if (_stage > scene.RequiredStage)
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

            // Advance progression cyclically:
            // After the required stage N successfully starts, the next allowed stage becomes (N + 1) % count,
            // which makes the sequence loop back to the first scene after the last one.
            _stage = (scene.RequiredStage + 1) % _scenes.Count;
        }

        #endregion
    }
}

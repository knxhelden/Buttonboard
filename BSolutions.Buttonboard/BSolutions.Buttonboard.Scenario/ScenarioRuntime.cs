using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.Runtime;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    /// <summary>
    /// Orchestrates the end-to-end execution of JSON-based scenes triggered by physical button inputs.
    /// </summary>
    /// <remarks>
    /// The <see cref="ScenarioRuntime"/> represents the central runtime loop of Buttonboard.  
    /// It continuously polls GPIO inputs, detects rising edges, and executes the corresponding
    /// <c>ScenarioAssetStep</c> sequences via <see cref="IScenarioAssetRuntime"/>.
    ///
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    /// <item><description>Rising-edge detection with mechanical debouncing.</description></item>
    /// <item><description>Stage-based progression of scenes (1→2→3→4, cyclic).</description></item>
    /// <item><description><c>TestOperation</c> mode (from <see cref="ApplicationOptions.DisableSceneOrder"/>):
    /// ignores stage order and allows free scene triggering.</description></item>
    /// </list>
    ///
    /// <para>
    /// The runtime manages LEDs and MQTT devices for feedback, ensuring all hardware components
    /// remain in sync with the current scenario state.
    /// </para>
    /// </remarks>
    public sealed class ScenarioRuntime : IScenarioRuntime
    {
        #region --- Fields ---

        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;
        private readonly IScenarioAssetRuntime _sceneRuntime;
        private readonly IButtonboardGpioController _gpio;
        private readonly IMqttClient _mqtt;

        private readonly bool _disableSceneOrder;
        private const int DebounceMs = 150;
        private const int PollDelayMs = 20;

        private int _stage = 0;
        private readonly Dictionary<Button, bool> _lastState = new();
        private readonly Dictionary<Button, long> _lastFireMs = new();
        private readonly List<SceneDef> _scenes;
        private readonly string _setupKey;

        #endregion

        #region --- Scene Model ---

        /// <summary>
        /// Represents a compact, immutable scene definition loaded from configuration.
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
        /// Initializes a new instance of the <see cref="ScenarioRuntime"/> class.
        /// </summary>
        /// <param name="logger">Logger for structured runtime diagnostics.</param>
        /// <param name="settings">Provides access to global application settings.</param>
        /// <param name="sceneRuntime">Service responsible for executing individual scenes.</param>
        /// <param name="gpio">GPIO controller for reading button input and controlling LEDs.</param>
        /// <param name="mqtt">MQTT client used for state synchronization and device feedback.</param>
        /// <param name="scenarioOptions">Bound configuration containing setup and scene definitions.</param>
        public ScenarioRuntime(
            ILogger<ScenarioRuntime> logger,
            ISettingsProvider settings,
            IScenarioAssetRuntime sceneRuntime,
            IButtonboardGpioController gpio,
            IMqttClient mqtt,
            IOptions<ScenarioOptions> scenarioOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sceneRuntime = sceneRuntime ?? throw new ArgumentNullException(nameof(sceneRuntime));
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
            _mqtt = mqtt ?? throw new ArgumentNullException(nameof(mqtt));

            _disableSceneOrder = _settings.Application.DisableSceneOrder;

            var opts = scenarioOptions.Value;
            _setupKey = string.IsNullOrWhiteSpace(opts.Setup.Key) ? "setup" : opts.Setup.Key;
            _scenes = opts.Scenes
                .Select(s => new SceneDef(s.Key, s.TriggerButton, s.RequiredStage))
                .ToList();

            foreach (var b in Enum.GetValues<Button>())
            {
                _lastState[b] = false;
                _lastFireMs[b] = -1;
            }
        }

        #endregion

        #region --- IScenarioRuntime Implementation ---

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is running… (TestOperation={Test})", _disableSceneOrder);
            await _gpio.LedOnAsync(Led.SystemGreen);

            var sw = Stopwatch.StartNew();

            try
            {
                while (!ct.IsCancellationRequested)
                {
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

        /// <inheritdoc />
        public async Task SetupAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being set up…");

            var started = await _sceneRuntime.StartAsync(_setupKey, ct);
            if (!started)
            {
                _logger.LogWarning("Setup scene not started (missing, busy, or error).");
            }
        }

        /// <inheritdoc />
        public async Task ResetAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Scenario is being reset…");

            await _sceneRuntime.CancelAsync();
            await _gpio.ResetAsync();
            await _mqtt.ResetAsync(ct);

            _stage = 0;
        }

        #endregion

        #region --- Private Helpers ---

        /// <summary>
        /// Detects a rising edge (button transition from not pressed → pressed) with debounce logic applied.
        /// Invokes the supplied asynchronous callback in a fire-and-forget manner.
        /// </summary>
        private void HandleButtonRisingEdge(Stopwatch sw, Button button, Func<Task> onPressedAsync)
        {
            var pressed = _gpio.IsButtonPressed(button);
            var wasPressed = _lastState[button];
            _lastState[button] = pressed;

            if (!pressed || wasPressed)
                return;

            var now = sw.ElapsedMilliseconds;
            var last = _lastFireMs[button];

            if (last >= 0 && (now - last) < DebounceMs)
                return;

            _lastFireMs[button] = now;
            _ = SafeFireAndForget(onPressedAsync);
        }

        /// <summary>
        /// Executes an asynchronous delegate and suppresses all exceptions.
        /// Errors must be logged within the delegate itself.
        /// </summary>
        private static async Task SafeFireAndForget(Func<Task> action)
        {
            try { await action().ConfigureAwait(false); }
            catch { /* intentionally ignored */ }
        }

        /// <summary>
        /// Attempts to trigger a scene according to the configured progression mode.
        /// </summary>
        /// <param name="scene">The scene definition to execute.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
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

            // Enforce stage order in normal mode.
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

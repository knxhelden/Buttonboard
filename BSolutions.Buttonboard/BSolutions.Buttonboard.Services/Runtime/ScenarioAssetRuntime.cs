using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime
{
    /// <summary>
    /// Default runtime implementation for executing a single <see cref="ScenarioAssetDefinition"/>.
    /// Looks up a scenario by key, schedules its steps by timestamp, and executes them via <see cref="IActionExecutor"/>.
    /// Supports cooperative cancellation and exposes simple runtime state.
    /// </summary>
    public sealed class ScenarioAssetRuntime : IScenarioAssetRuntime, IDisposable
    {
        #region --- Fields ---

        private readonly ILogger<ScenarioAssetRuntime> _logger;
        private readonly IScenarioAssetsLoader _loader;
        private readonly IActionExecutor _executor;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _runCts;
        private Task? _runTask;

        #endregion

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public string? CurrentSceneKey { get; private set; }

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="ScenarioAssetRuntime"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error reporting.</param>
        /// <param name="loader">Asset loader used to resolve definitions by key.</param>
        /// <param name="executor">Executor for individual asset steps.</param>
        public ScenarioAssetRuntime(
            ILogger<ScenarioAssetRuntime> logger,
            IScenarioAssetsLoader loader,
            IActionExecutor executor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        #endregion

        #region --- IScenarioAssetRuntime ---

        /// <inheritdoc />
        public async Task<bool> StartAsync(string sceneKey, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation(LogEvents.RuntimeStartIgnored,
                        "Start requested for {RequestedKey} but {CurrentKey} is still running",
                        sceneKey, CurrentSceneKey);
                    return false;
                }

                if (!_loader.TryGet(sceneKey, out var scene) || scene is null)
                {
                    _logger.LogWarning(LogEvents.RuntimeSceneMissing,
                        "Scene not found for key {SceneKey}", sceneKey);
                    return false;
                }

                IsRunning = true;
                CurrentSceneKey = sceneKey;

                _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var token = _runCts.Token;

                // Fire the run on a background task to keep StartAsync non-blocking.
                _runTask = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation(LogEvents.RuntimeStarted,
                            "Starting scene {Name} (v{Version}) with {StepCount} steps",
                            scene.Name, scene.Version, scene.Steps.Count);

                        var sw = Stopwatch.StartNew();

                        // Ensure strict time order (defensive: scene may already be sorted)
                        foreach (var step in scene.Steps.OrderBy(s => s.AtMs))
                        {
                            token.ThrowIfCancellationRequested();

                            // Calculate remaining delay relative to elapsed time
                            var delayMs = step.AtMs - (int)sw.ElapsedMilliseconds;
                            if (delayMs > 0)
                            {
                                await Task.Delay(delayMs, token).ConfigureAwait(false);
                            }

                            try
                            {
                                _logger.LogDebug(LogEvents.StepExecuting,
                                    "Executing step {StepName} at t~{AtMs}ms (Action {Action})",
                                    step.Name ?? "(unnamed)", step.AtMs, step.Action);

                                await _executor.ExecuteAsync(step, token).ConfigureAwait(false);

                                _logger.LogInformation(LogEvents.StepExecuted,
                                    "Step executed {StepName} (Action {Action}, AtMs {AtMs})",
                                    step.Name ?? "(unnamed)", step.Action, step.AtMs);
                            }
                            catch (OperationCanceledException)
                            {
                                throw; // handled by outer catch
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(LogEvents.StepFailed, ex,
                                    "Step failed {StepName} at t={AtMs}ms (Action {Action}; OnError {OnError})",
                                    step.Name ?? "(unnamed)", step.AtMs, step.Action, step.OnError);

                                // simple policy: continue unless OnError == "abort"
                                if (string.Equals(step.OnError, "abort", StringComparison.OrdinalIgnoreCase))
                                {
                                    break;
                                }
                            }
                        }

                        _logger.LogInformation(LogEvents.RuntimeFinished,
                            "Scene finished {Name}", scene.Name);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation(LogEvents.RuntimeCanceled,
                            "Scene canceled {SceneKey}", sceneKey);
                    }
                    finally
                    {
                        // Clear state under the gate to avoid races with StartAsync/CancelAsync.
                        await _gate.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            IsRunning = false;
                            CurrentSceneKey = null;
                            _runCts?.Dispose();
                            _runCts = null;
                            _runTask = null;
                        }
                        finally
                        {
                            _gate.Release();
                        }
                    }
                }, token);

                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsRunning)
                    return false;

                await CancelCoreAsync().ConfigureAwait(false);
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Requests cancellation and awaits the running task (best-effort).
        /// </summary>
        private async Task CancelCoreAsync()
        {
            try
            {
                _runCts?.Cancel();
            }
            catch
            {
                // Ignore exceptions when signaling cancellation.
            }

            var task = _runTask;
            if (task is not null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Exceptions are already logged in the run loop.
                }
            }
        }

        #endregion

        #region --- IDisposable ---

        /// <summary>
        /// Disposes internal resources and cancels any running scene.
        /// </summary>
        public void Dispose()
        {
            try { _runCts?.Cancel(); } catch { /* ignore */ }
            _runCts?.Dispose();
            _gate.Dispose();
        }

        #endregion
    }
}

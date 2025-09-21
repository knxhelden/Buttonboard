using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Default runtime for executing a single <see cref="ScenarioAssetDefinition"/>.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Looks up the requested asset from <see cref="IScenarioAssetsLoader"/></item>
    ///   <item>Schedules and executes all steps in time order via <see cref="IActionExecutor"/></item>
    ///   <item>Supports cooperative cancellation and error handling (continue/abort)</item>
    ///   <item>Tracks current runtime state (<see cref="IsRunning"/>, <see cref="CurrentSceneKey"/>)</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class ScenarioAssetRuntime : IScenarioAssetRuntime, IDisposable
    {
        private readonly ILogger<ScenarioAssetRuntime> _log;
        private readonly IScenarioAssetsLoader _loader;
        private readonly IActionExecutor _executor;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _runCts;
        private Task? _runTask;

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public string? CurrentSceneKey { get; private set; }

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="ScenarioAssetRuntime"/>.
        /// </summary>
        /// <param name="log">Logger for diagnostics and error reporting.</param>
        /// <param name="loader">Asset loader used to resolve definitions by key.</param>
        /// <param name="executor">Executor for individual asset steps.</param>
        public ScenarioAssetRuntime(ILogger<ScenarioAssetRuntime> log, IScenarioAssetsLoader loader, IActionExecutor executor)
        {
            _log = log;
            _loader = loader;
            _executor = executor;
        }

        #endregion

        #region --- IScenarioAssetRuntime ---

        /// <inheritdoc />
        /// <summary>
        /// Starts execution of the specified asset, if no other asset is currently running.
        /// </summary>
        /// <param name="sceneKey">Lookup key of the asset (file name without extension).</param>
        /// <param name="ct">Optional cancellation token to abort before the asset begins.</param>
        /// <returns>
        /// <c>true</c> if the asset was found and started; 
        /// <c>false</c> if another asset is still running or the key was not found.
        /// </returns>
        public async Task<bool> StartAsync(string sceneKey, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (IsRunning)
                {
                    _log.LogInformation("Scene '{SceneKey}' requested but '{Current}' is still running. Ignoring.", sceneKey, CurrentSceneKey);
                    return false;
                }

                if (!_loader.TryGet(sceneKey, out var scene) || scene is null)
                {
                    _log.LogWarning("Scene '{SceneKey}' not found.", sceneKey);
                    return false;
                }

                IsRunning = true;
                CurrentSceneKey = sceneKey;
                _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var token = _runCts.Token;

                _runTask = Task.Run(async () =>
                {
                    try
                    {
                        _log.LogInformation("Starting scene '{Name}' (v{Ver}) with {Count} steps.",
                            scene.Name, scene.Version, scene.Steps.Count);

                        var sw = Stopwatch.StartNew();

                        foreach (var step in scene.Steps.OrderBy(s => s.AtMs))
                        {
                            token.ThrowIfCancellationRequested();

                            var delay = step.AtMs - (int)sw.ElapsedMilliseconds;
                            if (delay > 0)
                                await Task.Delay(delay, token);

                            try
                            {
                                await _executor.ExecuteAsync(step, token);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "Step '{StepName}' failed at t={At}ms (action={Action})",
                                    step.Name ?? "(unnamed)", step.AtMs, step.Action);

                                // einfache Fehlerpolitik: „continue“ (Default), „abort“ optional via OnError
                                if (string.Equals(step.OnError, "abort", StringComparison.OrdinalIgnoreCase))
                                    break;
                            }
                        }

                        _log.LogInformation("Scene '{Name}' finished.", scene.Name);
                    }
                    catch (OperationCanceledException)
                    {
                        _log.LogInformation("Scene '{SceneKey}' canceled.", sceneKey);
                    }
                    finally
                    {
                        await _gate.WaitAsync();
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
                if (_gate.CurrentCount == 0) _gate.Release();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Cancels the currently running asset, if any.
        /// </summary>
        /// <returns>
        /// <c>true</c> if an asset was running and cancellation was requested;
        /// <c>false</c> if nothing was running.
        /// </returns>
        public async Task<bool> CancelAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (!IsRunning) return false;
                await CancelCoreAsync();
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        #endregion

        private async Task CancelCoreAsync()
        {
            try
            {
                _runCts?.Cancel();
            }
            catch { /* ignore */ }

            var task = _runTask;
            if (task is not null)
            {
                try { await task.ConfigureAwait(false); }
                catch { /* already handled in runner */ }
            }
        }

        /// <summary>
        /// Disposes internal resources such as the semaphore and cancellation token source.
        /// Cancels any currently running asset.
        /// </summary>
        public void Dispose()
        {
            _runCts?.Cancel();
            _runCts?.Dispose();
            _gate.Dispose();
        }
    }
}

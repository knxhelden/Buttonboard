using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public sealed class SceneRuntime : ISceneRuntime, IDisposable
    {
        private readonly ILogger<SceneRuntime> _log;
        private readonly IScenarioAssetsLoader _loader;
        private readonly IActionExecutor _executor;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private CancellationTokenSource? _runCts;
        private Task? _runTask;

        public bool IsRunning { get; private set; }
        public string? CurrentSceneKey { get; private set; }

        public SceneRuntime(ILogger<SceneRuntime> log, IScenarioAssetsLoader loader, IActionExecutor executor)
        {
            _log = log;
            _loader = loader;
            _executor = executor;
        }

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

        public void Dispose()
        {
            _runCts?.Cancel();
            _runCts?.Dispose();
            _gate.Dispose();
        }
    }
}

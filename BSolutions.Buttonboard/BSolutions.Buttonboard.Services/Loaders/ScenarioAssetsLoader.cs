using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Loaders
{
    /// <summary>
    /// Loader implementation for <see cref="IScenarioAssetsLoader"/>.
    /// Watches a directory for *.json and *.scene files, keeps a thread-safe in-memory cache,
    /// and normalizes assets (step filtering/sorting, kind detection).
    /// </summary>
    public sealed class ScenarioAssetsLoader : IScenarioAssetsLoader, IDisposable
    {
        #region --- Constants / Static ---

        private static readonly string[] SearchPatterns = new[] { "*.json", "*.scene" };
        private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

        #endregion

        #region --- Fields ---

        private readonly ILogger<ScenarioAssetsLoader> _logger;
        private readonly string _assetsDirectory;
        private readonly string _setupKey;
        private readonly FileSystemWatcher _watcher;

        private readonly ConcurrentDictionary<string, ScenarioAssetDefinition> _cache =
            new(KeyComparer);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        #endregion

        #region --- Constructor ---

        public ScenarioAssetsLoader(
            ILogger<ScenarioAssetsLoader> logger,
            string assetsDirectory,
            IOptions<ScenarioOptions> scenarioOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _assetsDirectory = assetsDirectory ?? throw new ArgumentNullException(nameof(assetsDirectory));

            var opts = scenarioOptions?.Value ?? throw new ArgumentNullException(nameof(scenarioOptions));
            _setupKey = string.IsNullOrWhiteSpace(opts.Setup?.Key) ? "setup" : opts.Setup!.Key;

            Directory.CreateDirectory(_assetsDirectory);

            // Watcher: we cannot set multiple filters, so use "*.*" and filter in code.
            _watcher = new FileSystemWatcher(_assetsDirectory)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, e) => { if (IsInteresting(e.FullPath)) _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath)); };
            _watcher.Created += (_, e) => { if (IsInteresting(e.FullPath)) _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath)); };
            _watcher.Renamed += (_, e) => { if (IsInteresting(e.FullPath)) _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath)); };
            _watcher.Deleted += (_, e) => { if (IsInteresting(e.FullPath)) RemoveAssetFromCache(e.FullPath); };
        }

        #endregion

        #region --- IScenarioAssetsLoader ---

        public async Task StartAsync(CancellationToken ct = default)
        {
            foreach (var file in EnumerateAllFiles(_assetsDirectory, SearchPatterns))
            {
                ct.ThrowIfCancellationRequested();
                await LoadAssetToCacheAsync(file).ConfigureAwait(false);
                await Task.Yield();
            }

            _watcher.EnableRaisingEvents = true;
            _logger.LogInformation(LogEvents.LoaderStarted,
                "ScenarioAssetsLoader started. Watching directory {Dir} for *.json and *.scene", _assetsDirectory);
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation(LogEvents.LoaderStopped, "ScenarioAssetsLoader stopped");
            return Task.CompletedTask;
        }

        public bool TryGet(string key, out ScenarioAssetDefinition? asset) =>
            _cache.TryGetValue(key, out asset);

        public bool TryGetSetup(out ScenarioAssetDefinition? setup) =>
            TryGet(_setupKey, out setup);

        public IEnumerable<string> Keys => _cache.Keys.ToArray();

        #endregion

        #region --- Helpers ---

        private static IEnumerable<string> EnumerateAllFiles(string dir, IEnumerable<string> patterns)
        {
            foreach (var p in patterns)
                foreach (var f in Directory.EnumerateFiles(dir, p))
                    yield return f;
        }

        private static bool IsInteresting(string path)
        {
            var ext = Path.GetExtension(path);
            return ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".scene", StringComparison.OrdinalIgnoreCase);
        }

        private async Task LoadAssetToCacheAsync(string path)
        {
            try
            {
                await Task.Delay(75).ConfigureAwait(false);
                if (!File.Exists(path)) return;

                var key = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                _logger.LogDebug("Loading scenario asset {Key} from {Path}…", key, path);

                string raw;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                    raw = await sr.ReadToEndAsync().ConfigureAwait(false);

                ScenarioAssetDefinition? def = null;

                if (ext == ".json")
                {
                    try
                    {
                        def = System.Text.Json.JsonSerializer.Deserialize<ScenarioAssetDefinition>(raw, _jsonOptions);
                    }
                    catch (System.Text.Json.JsonException jx)
                    {
                        _logger.LogWarning(LogEvents.AssetJsonInvalid, jx,
                            "JSON parse failed for {Path}. JsonPath={JsonPath}", path, jx.Path);
                        return;
                    }
                }
                else if (ext == ".scene")
                {
                    try
                    {
                        def = SceneDslParser.ParseToAssetDefinition(raw, key, _setupKey, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(LogEvents.AssetJsonInvalid, ex,
                            "Scene DSL parse failed for {Path}. Error={Message}", path, ex.Message);
                        return;
                    }
                }

                if (def is null) return;

                def = Normalize(def, key, _setupKey);
                _cache[key] = def;

                _logger.LogInformation(LogEvents.AssetLoaded,
                    "Scenario asset loaded: {Key} (Kind {Kind}, Steps {StepCount})",
                    key, def.Kind, def.Steps?.Count ?? 0);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Loading scenario asset canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.AssetLoadError, ex,
                    "Error while loading scenario asset. Path={Path}", path);
            }
        }

        private void RemoveAssetFromCache(string path)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (_cache.TryRemove(key, out _))
                    _logger.LogInformation(LogEvents.AssetRemoved, "Scenario asset removed: {Key}", key);
            }
            catch { /* best-effort */ }
        }

        private static ScenarioAssetDefinition Normalize(ScenarioAssetDefinition def, string key, string setupKey)
        {
            def.Name ??= key;
            def.Steps ??= new();
            def.Steps = def.Steps
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Action))
                .OrderBy(s => s.AtMs)
                .ToList();

            def.Kind = string.Equals(key, setupKey, StringComparison.OrdinalIgnoreCase)
                ? ScenarioAssetKind.Setup
                : ScenarioAssetKind.Scene;

            return def;
        }

        public void Dispose() => _watcher.Dispose();

        #endregion
    }
}

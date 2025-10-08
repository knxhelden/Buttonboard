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
    /// Watches a directory for <c>*.json</c> files, keeps a thread-safe in-memory cache,
    /// and normalizes assets (step filtering/sorting, kind detection).
    /// </summary>
    public sealed class ScenarioAssetsLoader : IScenarioAssetsLoader, IDisposable
    {
        #region --- Constants / Static ---

        private const string JsonSearchPattern = "*.json";
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

        /// <summary>
        /// Creates a new <see cref="ScenarioAssetsLoader"/>.
        /// </summary>
        /// <param name="logger">Logger instance for this loader.</param>
        /// <param name="assetsDirectory">Directory that contains the scenario JSON files.</param>
        /// <param name="scenarioOptions">Options to resolve the setup key.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsDirectory"/> is null.</exception>
        public ScenarioAssetsLoader(
            ILogger<ScenarioAssetsLoader> logger,
            string assetsDirectory,
            IOptions<ScenarioOptions> scenarioOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _assetsDirectory = assetsDirectory ?? throw new ArgumentNullException(nameof(assetsDirectory));

            var opts = scenarioOptions?.Value ?? throw new ArgumentNullException(nameof(scenarioOptions));
            _setupKey = string.IsNullOrWhiteSpace(opts.Setup?.Key) ? "setup" : opts.Setup.Key;

            // Ensure directory exists (no-op if already present).
            Directory.CreateDirectory(_assetsDirectory);

            // Configure file watcher (no events until StartAsync enables it).
            _watcher = new FileSystemWatcher(_assetsDirectory, JsonSearchPattern)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            // Use background tasks to avoid blocking the watcher event thread.
            _watcher.Changed += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Created += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Renamed += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Deleted += (_, e) => RemoveAssetFromCache(e.FullPath);
        }

        #endregion

        #region --- IScenarioAssetsLoader ---

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken ct = default)
        {
            foreach (var file in Directory.EnumerateFiles(_assetsDirectory, JsonSearchPattern))
            {
                ct.ThrowIfCancellationRequested();
                await LoadAssetToCacheAsync(file).ConfigureAwait(false);
                await Task.Yield(); // small pacing to keep startup responsive
            }

            _watcher.EnableRaisingEvents = true;

            _logger.LogInformation(LogEvents.LoaderStarted,
                "ScenarioAssetsLoader started. Watching directory {Dir}", _assetsDirectory);
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken ct = default)
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation(LogEvents.LoaderStopped, "ScenarioAssetsLoader stopped");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public bool TryGet(string key, out ScenarioAssetDefinition? asset) =>
            _cache.TryGetValue(key, out asset);

        /// <inheritdoc />
        public bool TryGetSetup(out ScenarioAssetDefinition? setup) =>
            TryGet(_setupKey, out setup);

        /// <inheritdoc />
        public IEnumerable<string> Keys => _cache.Keys.ToArray(); // snapshot to avoid enumeration issues

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Loads or reloads a single asset file into the cache.
        /// </summary>
        private async Task LoadAssetToCacheAsync(string path)
        {
            try
            {
                // Debounce: file editors may trigger multiple rapid events.
                await Task.Delay(75).ConfigureAwait(false);

                if (!File.Exists(path))
                    return;

                var key = Path.GetFileNameWithoutExtension(path);
                _logger.LogDebug("Loading scenario asset {Key} from {Path}…", key, path);

                // Share read to allow editors to keep the file open.
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var json = await sr.ReadToEndAsync().ConfigureAwait(false);

                ScenarioAssetDefinition? def;
                try
                {
                    def = JsonSerializer.Deserialize<ScenarioAssetDefinition>(json, _jsonOptions);
                }
                catch (JsonException jx)
                {
                    // Parsing errors are expected during editing; log as Warning with JSON path context.
                    _logger.LogWarning(LogEvents.AssetJsonInvalid, jx,
                        "JSON parse failed for {Path}. JsonPath={JsonPath}", path, jx.Path);
                    return;
                }

                if (def is null)
                    return;

                def = Normalize(def, key, _setupKey);
                _cache[key] = def;

                _logger.LogInformation(LogEvents.AssetLoaded,
                    "Scenario asset loaded: {Key} (Kind {Kind}, Steps {StepCount})",
                    key, def.Kind, def.Steps?.Count ?? 0);
            }
            catch (OperationCanceledException)
            {
                // If a token is introduced in the future, a canceled load is not an error.
                _logger.LogInformation("Loading scenario asset canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.AssetLoadError, ex,
                    "Error while loading scenario asset. Path={Path}", path);
            }
        }

        /// <summary>
        /// Removes an asset from the cache if present.
        /// </summary>
        private void RemoveAssetFromCache(string path)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (_cache.TryRemove(key, out _))
                {
                    _logger.LogInformation(LogEvents.AssetRemoved, "Scenario asset removed: {Key}", key);
                }
            }
            catch
            {
                // Best-effort removal: ignore unexpected exceptions.
            }
        }

        /// <summary>
        /// Ensures a consistent shape: filters invalid steps, sorts by time and sets the asset kind.
        /// </summary>
        private static ScenarioAssetDefinition Normalize(ScenarioAssetDefinition def, string key, string setupKey)
        {
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

        #endregion

        #region --- IDisposable ---

        /// <inheritdoc />
        public void Dispose() => _watcher.Dispose();

        #endregion
    }
}

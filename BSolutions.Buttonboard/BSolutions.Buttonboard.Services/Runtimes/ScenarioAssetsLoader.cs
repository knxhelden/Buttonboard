using BSolutions.Buttonboard.Services.Enumerations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Loads scenario assets (scenes and setup) from JSON files into a thread-safe cache
    /// and keeps them up-to-date using a FileSystemWatcher (hot-reload).
    /// The lookup key is the file name without the ".json" extension.
    /// </summary>
    public sealed class ScenarioAssetsLoader : IScenarioAssetsLoader, IDisposable
    {
        public static class WellKnownKeys
        {
            public const string Setup = "setup";
        }

        private readonly ILogger<ScenarioAssetsLoader> _logger;
        private readonly string _assetsDirectory;
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, ScenarioAssetDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        public ScenarioAssetsLoader(ILogger<ScenarioAssetsLoader> logger, string assetsDirectory)
        {
            _logger = logger;
            _assetsDirectory = assetsDirectory ?? throw new ArgumentNullException(nameof(assetsDirectory));
            Directory.CreateDirectory(_assetsDirectory);

            _watcher = new FileSystemWatcher(_assetsDirectory, "*.json")
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Created += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Renamed += (_, e) => _ = Task.Run(() => LoadAssetToCacheAsync(e.FullPath));
            _watcher.Deleted += (_, e) => RemoveAssetFromCache(e.FullPath);
        }

        #region IScenarioAssetsLoader

        public async Task StartAsync(CancellationToken ct = default)
        {
            foreach (var file in Directory.EnumerateFiles(_assetsDirectory, "*.json"))
            {
                ct.ThrowIfCancellationRequested();
                await LoadAssetToCacheAsync(file);
                await Task.Yield(); // pacing
            }

            _watcher.EnableRaisingEvents = true;
            _logger.LogInformation("ScenarioAssetsLoader started. Watching directory: {Dir}", _assetsDirectory);
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation("ScenarioAssetsLoader stopped.");
            return Task.CompletedTask;
        }

        public bool TryGet(string key, out ScenarioAssetDefinition? asset) => _cache.TryGetValue(key, out asset);

        public bool TryGetSetup(out ScenarioAssetDefinition? setup) =>
            TryGet(WellKnownKeys.Setup, out setup);

        public IEnumerable<string> Keys => _cache.Keys;

        #endregion

        private async Task LoadAssetToCacheAsync(string path)
        {
            try
            {
                await Task.Delay(75); // small debounce for editors

                if (!File.Exists(path)) return;

                var key = Path.GetFileNameWithoutExtension(path);
                _logger.LogInformation("Loading scenario asset '{Key}' from '{Path}'…", key, path);

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var json = await sr.ReadToEndAsync();

                ScenarioAssetDefinition? def;
                try
                {
                    def = JsonSerializer.Deserialize<ScenarioAssetDefinition>(json, _jsonOptions);
                }
                catch (JsonException jx)
                {
                    _logger.LogWarning(jx, "JSON parse failed for {Path}. JsonPath={JsonPath}", path, jx.Path);
                    return;
                }

                if (def is null) return;

                _cache[key] = Normalize(def, key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while loading scenario asset (Path: {Path})", path);
            }
        }

        private void RemoveAssetFromCache(string path)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (_cache.TryRemove(key, out _))
                {
                    _logger.LogDebug("Scenario asset removed: {Key}", key);
                }
            }
            catch
            {
                // ignore
            }
        }

        private ScenarioAssetDefinition Normalize(ScenarioAssetDefinition def, string key)
        {
            def.Steps ??= new();
            def.Steps = def.Steps
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Action))
                .OrderBy(s => s.AtMs)
                .ToList();

            def.Kind = string.Equals(key, WellKnownKeys.Setup, StringComparison.OrdinalIgnoreCase)
                ? ScenarioAssetKind.Setup
                : ScenarioAssetKind.Scene;

            return def;
        }

        public void Dispose() => _watcher.Dispose();
    }
}

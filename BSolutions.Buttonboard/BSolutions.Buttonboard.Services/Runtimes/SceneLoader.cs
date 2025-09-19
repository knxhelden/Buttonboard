using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization.Metadata;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Loads scene definitions from JSON files into a thread-safe cache and
    /// keeps them up-to-date using a FileSystemWatcher (hot-reload).
    /// The lookup key is the file name without the ".json" extension.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader, IDisposable
    {
        private readonly ILogger<SceneLoader> _logger;
        private readonly string _scenesDirectory;
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, SceneDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="SceneLoader"/>.
        /// </summary>
        /// <param name="logger">Logger used for warnings and errors.</param>
        /// <param name="scenesDirectory">The directory containing JSON scene files.</param>
        public SceneLoader(ILogger<SceneLoader> logger, string scenesDirectory)
        {
            _logger = logger;
            _scenesDirectory = scenesDirectory ?? throw new ArgumentNullException(nameof(scenesDirectory));
            Directory.CreateDirectory(_scenesDirectory);

            _watcher = new FileSystemWatcher(_scenesDirectory, "*.json")
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, e) => _ = Task.Run(() => LoadSceneToCacheAsync(e.FullPath));
            _watcher.Created += (_, e) => _ = Task.Run(() => LoadSceneToCacheAsync(e.FullPath));
            _watcher.Renamed += (_, e) => _ = Task.Run(() => LoadSceneToCacheAsync(e.FullPath));
            _watcher.Deleted += (_, e) => RemoveSceneFromCache(e.FullPath);
        }

        #endregion

        #region --- ISceneLoader ---

        /// <summary>
        /// Starts the loader: performs an initial scan/load and then enables hot-reload.
        /// </summary>
        public async Task StartAsync(CancellationToken ct = default)
        {
            // Initial load
            foreach (var file in Directory.EnumerateFiles(_scenesDirectory, "*.json"))
            {
                ct.ThrowIfCancellationRequested();
                await LoadSceneToCacheAsync(file);
                // Cooperative pacing if there are many files
                await Task.Yield();
            }

            _watcher.EnableRaisingEvents = true;
            _logger.LogInformation("SceneLoader started. Watching directory: {Dir}", _scenesDirectory);
        }

        /// <summary>
        /// Stops the loader: disables file watching.
        /// </summary>
        public Task StopAsync(CancellationToken ct = default)
        {
            _watcher.EnableRaisingEvents = false;
            _logger.LogInformation("SceneLoader stopped.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tries to get a scene by key (file name without the ".json" extension).
        /// </summary>
        public bool TryGet(string sceneKey, out SceneDefinition? scene)
        {
            return _cache.TryGetValue(sceneKey, out scene);
        }

        #endregion

        /// <summary>
        /// Loads a scene from the given file path and updates the cache (upsert).
        /// Intended to be called from FileSystemWatcher events (Changed/Created/Renamed).
        /// </summary>
        private async Task LoadSceneToCacheAsync(string path)
        {
            try
            {
                // Small debounce to let editors finish writing
                await Task.Delay(50);

                if (!File.Exists(path)) return;

                _logger.LogInformation("Loading scene from path '{0}'…", path);

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var json = await sr.ReadToEndAsync();

                var def = JsonSerializer.Deserialize<SceneDefinition>(json, _jsonOptions);
                if (def is null) return;

                var key = Path.GetFileNameWithoutExtension(path);
                _cache[key] = Normalize(def);
                _logger.LogInformation("Scene loaded: {Key}.", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occurred while loading a scene (Path: {Path})", path);
            }
        }

        /// <summary>
        /// Removes a scene from the cache when its file is deleted.
        /// </summary>
        private void RemoveSceneFromCache(string path)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (_cache.TryRemove(key, out _))
                {
                    _logger.LogDebug("Scene removed: {Key}", key);
                }
            }
            catch
            {
                // Intentionally ignore
            }
        }

        /// <summary>
        /// Normalizes a loaded scene: null-safety, filtering invalid steps,
        /// and sorting by start time in milliseconds since scene start.
        /// </summary>
        private SceneDefinition Normalize(SceneDefinition def)
        {
            def.Steps ??= new();

            def.Steps = def.Steps
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Action))
                .OrderBy(s => s.StartAtMs)
                .ToList();

            return def;
        }

        /// <summary>
        /// Disposes the underlying FileSystemWatcher.
        /// </summary>
        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}

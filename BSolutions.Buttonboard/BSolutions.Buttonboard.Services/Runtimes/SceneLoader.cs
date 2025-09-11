using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Loads scene definitions from JSON files into a thread-safe cache and
    /// keeps them up-to-date using a FileSystemWatcher (hot-reload).
    /// The lookup key is the file name without the ".json" extension.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader, IDisposable
    {
        private readonly ILogger<OpenHabClient> _logger;
        private readonly string _scenesDirectory;
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, SceneDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="SceneLoader"/>.
        /// </summary>
        /// <param name="logger">Logger used for warnings and errors.</param>
        /// <param name="scenesDirectory">The name of the scene directory.</param>
        public SceneLoader(ILogger<SceneLoader> logger, string scenesDirectory)
        {
            _scenesDirectory = scenesDirectory;
            Directory.CreateDirectory(_scenesDirectory);

            // Instantiate scenes file system watcher
            _watcher = new FileSystemWatcher(_scenesDirectory, "*.json")
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, e) => LoadSceneToCache(e.FullPath);
            _watcher.Created += (_, e) => LoadSceneToCache(e.FullPath);
            _watcher.Renamed += (_, e) => LoadSceneToCache(e.FullPath);
            _watcher.Deleted += (_, e) => RemoveSceneFromCache(e.FullPath);
        }

        #endregion

        #region --- ISceneLoader ---

        /// <summary>
        /// Starts the loader: performs an initial scan/load and then enables hot-reload.
        /// </summary>
        public void Start()
        {
            // Initial load
            foreach (var file in Directory.EnumerateFiles(_scenesDirectory, "*.json"))
            {
                LoadSceneToCache(file);
            }

            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Tries to get a scene by key (file name without the ".json" extension).
        /// </summary>
        /// <param name="sceneKey">File name without extension, e.g. "horrorhouse.scene2".</param>
        /// <param name="scene">Outputs the scene if present, otherwise null.</param>
        /// <returns>True if the scene is present in the cache; otherwise false.</returns>
        public bool TryGet(string sceneKey, out SceneDefinition? scene)
        {
            return _cache.TryGetValue(sceneKey, out scene);
        }

        #endregion

        /// <summary>
        /// Loads a scene from the given file path and updates the cache (upsert).
        /// Intended to be called from FileSystemWatcher events (Changed/Created/Renamed).
        /// </summary>
        /// <param name="path">Full path of the JSON scene file.</param>
        private void LoadSceneToCache(string path)
        {
            try
            {
                // Small delay if editor is still writing in memory
                Task.Delay(50).Wait();
                if (!File.Exists(path))
                    return;

                var json = File.ReadAllText(path);
                var def = JsonSerializer.Deserialize<SceneDefinition>(json, _jsonOptions);
                if (def is null) return;

                var key = Path.GetFileNameWithoutExtension(path);
                _cache[key] = Normalize(def);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occurred in a scene configuration (Path: {0})", path);
            }
        }

        /// <summary>
        /// Removes a scene from the cache when its file is deleted.
        /// </summary>
        /// <param name="path">Full path of the deleted file.</param>
        private void RemoveSceneFromCache(string path)
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(path);
                _cache.TryRemove(key, out _);
            }
            catch { }
        }

        /// <summary>
        /// Normalizes a loaded scene: null-safety, filtering invalid steps,
        /// and sorting by start time in milliseconds since scene start.
        /// </summary>
        /// <param name="def">Raw <see cref="SceneDefinition"/> as deserialized.</param>
        /// <returns>A cleaned and sorted <see cref="SceneDefinition"/>.</returns>
        private SceneDefinition Normalize(SceneDefinition def)
        {
            // If Steps is null, create empty list
            def.Steps ??= new();

            // Filter out invalid steps and sort by atMs
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

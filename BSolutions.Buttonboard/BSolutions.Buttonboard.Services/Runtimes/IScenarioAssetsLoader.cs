using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Provides access to all <b>scenario assets</b> (scenes and setup) stored as JSON files.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Initial scan of a configured directory (e.g. <c>scenes/</c>)</item>
    ///   <item>Hot-reload via <see cref="FileSystemWatcher"/> to reflect file changes at runtime</item>
    ///   <item>Thread-safe lookup of loaded scene/setup definitions by key</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Key convention:</b> The lookup key is the file name without the <c>.json</c> extension.
    /// For example, <c>scene1.json</c> → <c>"scene1"</c>, <c>Setup.json</c> → <c>"setup"</c>.
    /// </para>
    /// </summary>
    public interface IScenarioAssetsLoader : IDisposable
    {
        /// <summary>
        /// Starts the loader: performs an initial scan of all <c>*.json</c> files 
        /// in the configured directory and then enables hot-reload for subsequent changes.
        /// </summary>
        /// <param name="ct">Cancellation token to stop the initial scan early.</param>
        Task StartAsync(CancellationToken ct = default);

        /// <summary>
        /// Stops the loader: disables file watching. 
        /// Already cached definitions remain available until disposal.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task StopAsync(CancellationToken ct = default);

        /// <summary>
        /// Tries to get a scenario asset definition (scene or setup) by its lookup key.
        /// </summary>
        /// <param name="sceneKey">File name without <c>.json</c> (case-insensitive).</param>
        /// <param name="scene">Returns the loaded <see cref="ScenarioAssetDefinition"/> if found.</param>
        /// <returns><c>true</c> if the asset is loaded and returned; otherwise <c>false</c>.</returns>
        bool TryGet(string sceneKey, out ScenarioAssetDefinition? scene);

        /// <summary>
        /// Shortcut for accessing the <c>Setup.json</c> asset.
        /// Equivalent to <c>TryGet("setup", out …)</c>.
        /// </summary>
        /// <param name="setup">Returns the setup definition if available.</param>
        /// <returns><c>true</c> if <c>Setup.json</c> is loaded and returned; otherwise <c>false</c>.</returns>
        bool TryGetSetup(out ScenarioAssetDefinition? setup);

        /// <summary>
        /// Enumerates all currently loaded keys. 
        /// Useful for diagnostics, tooling or building a UI.
        /// </summary>
        IEnumerable<string> Keys { get; }
    }
}

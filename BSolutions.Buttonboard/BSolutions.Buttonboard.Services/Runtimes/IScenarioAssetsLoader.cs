using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public interface IScenarioAssetsLoader : IDisposable
    {
        /// <summary>
        /// Starts the loader: performs an initial scan/load and then enables hot-reload.
        /// </summary>
        Task StartAsync(CancellationToken ct = default);

        /// <summary>
        /// Stops the loader
        /// </summary>
        Task StopAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the current definition of a scene (file name without extension).
        /// </summary>
        bool TryGet(string sceneKey, out SceneDefinition? scene);

        /// <summary>
        /// Convenience for Setup.json → key "setup".
        /// </summary>
        bool TryGetSetup(out SceneDefinition? setup);

        /// <summary>
        /// All currently loaded keys (diagnostics, UX).
        /// </summary>
        IEnumerable<string> Keys { get; }
    }
}

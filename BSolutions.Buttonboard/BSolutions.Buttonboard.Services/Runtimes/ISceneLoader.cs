using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public interface ISceneLoader : IDisposable
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
    }
}

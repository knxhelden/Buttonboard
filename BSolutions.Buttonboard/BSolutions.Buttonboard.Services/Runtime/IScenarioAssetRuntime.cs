using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime
{
    /// <summary>
    /// Runtime abstraction for executing a single <b>scenario asset</b> 
    /// (e.g. a scene or the special setup).
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Start execution of an asset by its lookup key</item>
    ///   <item>Cancel the currently running asset</item>
    ///   <item>Provide runtime state information for diagnostics and orchestration</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface IScenarioAssetRuntime
    {
        /// <summary>
        /// Starts execution of the specified asset.
        /// </summary>
        /// <param name="sceneKey">
        /// Lookup key of the asset (file name without <c>.json</c>, 
        /// e.g. <c>"scene1"</c> or <c>"setup"</c>).
        /// </param>
        /// <param name="ct">Cancellation token for aborting startup before the asset begins.</param>
        /// <returns>
        /// <c>true</c> if the asset was started; 
        /// <c>false</c> if another asset is already running or startup was denied.
        /// </returns>
        Task<bool> StartAsync(string sceneKey, CancellationToken ct = default);

        /// <summary>
        /// Cancels the currently running asset, if any.
        /// </summary>
        /// <returns>
        /// <c>true</c> if an asset was running and got cancelled; 
        /// <c>false</c> if no asset was running.
        /// </returns>
        Task<bool> CancelAsync();

        /// <summary>
        /// Indicates whether an asset is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the lookup key of the currently running asset, or <c>null</c> if none is active.
        /// </summary>
        string? CurrentSceneKey { get; }
    }
}

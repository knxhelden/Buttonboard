using BSolutions.Buttonboard.Services.Loaders;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Defines a contract for routing and executing actions within a specific domain.
    /// </summary>
    /// <remarks>
    /// Each implementation of <see cref="IActionRouter"/> is responsible for handling
    /// a distinct action domain (e.g., <c>"audio"</c>, <c>"video"</c>, <c>"gpio"</c>).
    /// Routers are registered with the <see cref="IActionRouterRegistry"/> and resolved
    /// dynamically by the <see cref="ActionExecutor"/> at runtime.
    /// </remarks>
    public interface IActionRouter
    {
        /// <summary>
        /// Gets the logical domain that this router handles (e.g., <c>"audio"</c> or <c>"gpio"</c>).
        /// </summary>
        string Domain { get; }

        /// <summary>
        /// Determines whether this router can handle the specified action key.
        /// </summary>
        /// <param name="actionKey">The normalized action key (e.g., <c>"audio.play"</c>).</param>
        /// <returns>
        /// <c>true</c> if this router is responsible for the given action; otherwise <c>false</c>.
        /// </returns>
        bool CanHandle(string actionKey);

        /// <summary>
        /// Executes the given scenario step asynchronously.
        /// </summary>
        /// <param name="step">The <see cref="ScenarioAssetStep"/> containing the action and its arguments.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> for cooperative cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous execution.</returns>
        Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct);
    }
}

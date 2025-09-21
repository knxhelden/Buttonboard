using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    /// <summary>
    /// Executes a single <see cref="ScenarioAssetStep"/> within a scenario asset.
    /// <para>
    /// Implementations interpret the <see cref="ScenarioAssetStep.Action"/> and its
    /// <see cref="ScenarioAssetStep.Args"/> to perform the corresponding side effect,
    /// such as controlling GPIO, sending MQTT messages, or triggering audio/video playback.
    /// </para>
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        /// Executes the given step.
        /// </summary>
        /// <param name="step">
        /// The <see cref="ScenarioAssetStep"/> to execute.
        /// Contains the action identifier, optional arguments, and error-handling policy.
        /// </param>
        /// <param name="ct">
        /// Cancellation token that should be honored by long-running actions
        /// (e.g. network operations or blocking I/O). If cancellation is requested,
        /// the action should stop gracefully as soon as possible.
        /// </param>
        /// <returns>
        /// A task that completes once the step has been processed.
        /// </returns>
        /// <remarks>
        /// Implementations are expected to:
        /// <list type="bullet">
        ///   <item>Validate that <paramref name="step"/> is supported.</item>
        ///   <item>Throw an exception if execution fails (caller will interpret <c>OnError</c>).</item>
        ///   <item>Log relevant information for observability.</item>
        /// </list>
        /// </remarks>
        Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct);
    }
}

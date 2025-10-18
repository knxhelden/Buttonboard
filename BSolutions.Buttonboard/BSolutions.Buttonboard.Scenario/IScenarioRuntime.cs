using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    /// <summary>
    /// Defines the contract for managing the lifecycle of a Buttonboard scenario.
    /// </summary>
    /// <remarks>
    /// A scenario represents a complete sequence of <c>ScenarioAssetStep</c> actions
    /// (e.g., lighting, audio, video, or MQTT events) defined in the loaded setup.
    /// 
    /// The <see cref="IScenarioRuntime"/> orchestrates initialization, execution, and cleanup
    /// of these scenarios and provides explicit lifecycle control through its three core methods:
    /// <list type="bullet">
    /// <item><description><see cref="SetupAsync"/> – Prepares all assets, initializes devices, and validates configuration.</description></item>
    /// <item><description><see cref="RunAsync"/> – Executes the scenario sequence asynchronously.</description></item>
    /// <item><description><see cref="ResetAsync"/> – Resets state and restores a defined baseline after completion or interruption.</description></item>
    /// </list>
    /// </remarks>
    public interface IScenarioRuntime
    {
        /// <summary>
        /// Executes the currently loaded scenario sequence asynchronously.
        /// </summary>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> for cooperative cancellation.
        /// When requested, the scenario should stop execution gracefully.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous scenario execution.
        /// </returns>
        Task RunAsync(CancellationToken ct = default);

        /// <summary>
        /// Initializes and prepares the scenario environment before execution.
        /// </summary>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> for cooperative cancellation during setup.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous setup operation.
        /// </returns>
        Task SetupAsync(CancellationToken ct = default);

        /// <summary>
        /// Resets the scenario to its initial state, typically after completion or interruption.
        /// </summary>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> for cooperative cancellation during reset.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous reset operation.
        /// </returns>
        Task ResetAsync(CancellationToken ct = default);
    }
}

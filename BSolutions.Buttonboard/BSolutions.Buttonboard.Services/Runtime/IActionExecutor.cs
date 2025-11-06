using BSolutions.Buttonboard.Services.Loaders;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime
{
    /// <summary>
    /// Defines a contract for executing a single <see cref="ScenarioStepDefinition"/> within a scenario.
    /// </summary>
    /// <remarks>
    /// Implementations interpret the <see cref="ScenarioStepDefinition.Action"/> and its
    /// associated <see cref="ScenarioStepDefinition.Args"/> to perform the corresponding effect —
    /// such as controlling GPIOs, publishing MQTT messages, or triggering audio/video playback.
    ///
    /// The <see cref="IActionExecutor"/> serves as the central dispatch component that
    /// delegates execution to the appropriate <c>IActionRouter</c> based on the action domain.
    ///
    /// Implementations are expected to:
    /// <list type="bullet">
    ///   <item><description>Validate whether the action is supported and well-formed.</description></item>
    ///   <item><description>Throw descriptive exceptions when execution fails (caller interprets <c>OnError</c> policy).</description></item>
    ///   <item><description>Log structured diagnostic information for observability.</description></item>
    /// </list>
    /// </remarks>
    public interface IActionExecutor
    {
        /// <summary>
        /// Executes the given scenario step asynchronously.
        /// </summary>
        /// <param name="step">
        /// The <see cref="ScenarioStepDefinition"/> to execute.
        /// Contains the action identifier, arguments, and error-handling policy.
        /// </param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> for cooperative cancellation.
        /// Long-running actions (e.g., network or I/O operations) should terminate gracefully when cancelled.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that completes once the step has been executed.
        /// </returns>
        Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct);
    }
}

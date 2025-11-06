using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Runtime.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime
{
    /// <summary>
    /// Thin dispatcher that routes scenario actions to their corresponding domain routers.
    /// </summary>
    /// <remarks>
    /// The <see cref="ActionExecutor"/> serves as the central entry point for executing
    /// <see cref="ScenarioStepDefinition"/> instances. It determines the responsible
    /// <see cref="IActionRouter"/> by inspecting the action key prefix (e.g., <c>audio.play</c>, <c>gpio.on</c>)
    /// and delegates execution to it.
    ///
    /// Responsibilities:
    /// <list type="bullet">
    /// <item><description>Normalize and validate the incoming <see cref="ScenarioStepDefinition"/>.</description></item>
    /// <item><description>Resolve the correct <see cref="IActionRouter"/> via <see cref="IActionRouterRegistry"/>.</description></item>
    /// <item><description>Log routing decisions and unknown actions for observability.</description></item>
    /// </list>
    ///
    /// The executor itself contains no domain logic — all behavior is delegated to routers.
    /// </remarks>
    public sealed class ActionExecutor : IActionExecutor
    {
        private readonly ILogger _logger;
        private readonly IActionRouterRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger used for structured runtime diagnostics.</param>
        /// <param name="registry">The router registry used to resolve domain-specific action handlers.</param>
        public ActionExecutor(
            ILogger<ActionExecutor> logger,
            IActionRouterRegistry registry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            if (step is null)
                throw new ArgumentNullException(nameof(step));

            var key = step.Action?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction,
                    "Unknown action {Action}", "(null/empty)");
                return;
            }

            if (!_registry.TryResolve(key, out var router))
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction,
                    "Unknown action {Action}", key);
                return;
            }

            await router.ExecuteAsync(step, ct).ConfigureAwait(false);
        }
    }
}

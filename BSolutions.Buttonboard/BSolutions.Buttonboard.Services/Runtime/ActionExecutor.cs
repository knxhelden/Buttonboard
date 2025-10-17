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
    /// Thin dispatcher that resolves concrete IActionHandler implementations by action key.
    /// Keeps the executor minimal and extensible.
    /// </summary>
    public sealed class ActionExecutor : IActionExecutor
    {
        private readonly ILogger _logger;
        private readonly IActionHandlerRegistry _registry;

        public ActionExecutor(ILogger<ActionExecutor> logger,
                              IActionHandlerRegistry registry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            if (step is null) throw new ArgumentNullException(nameof(step));

            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", "(null/empty)");
                return; // tolerant
            }

            if (!_registry.TryResolve(key, out var handler) || handler is null)
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", step.Action);
                return; // tolerant
            }

            await handler.ExecuteAsync(step, ct).ConfigureAwait(false);
        }
    }
}

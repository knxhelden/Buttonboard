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
    /// Thin dispatcher, der einen Domain-Router anhand des Action-Präfixes aufruft.
    /// </summary>
    public sealed class ActionExecutor : IActionExecutor
    {
        private readonly ILogger _logger;
        private readonly IActionRouterRegistry _registry;

        public ActionExecutor(ILogger<ActionExecutor> logger,
                              IActionRouterRegistry registry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct)
        {
            if (step is null) throw new ArgumentNullException(nameof(step));

            var key = step.Action?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", "(null/empty)");
                return;
            }

            if (!_registry.TryResolve(key, out var router))
            {
                _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown action {Action}", key);
                return;
            }

            await router.ExecuteAsync(step, ct).ConfigureAwait(false);
        }
    }
}

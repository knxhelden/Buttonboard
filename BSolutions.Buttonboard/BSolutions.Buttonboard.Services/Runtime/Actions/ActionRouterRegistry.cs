using System;
using System.Collections.Generic;
using System.Linq;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Default implementation of <see cref="IActionRouterRegistry"/>.
    /// Maintains an internal collection of registered <see cref="IActionRouter"/> instances and
    /// performs case-insensitive lookups to resolve the correct router for a given action key.
    /// </summary>
    public sealed class ActionRouterRegistry : IActionRouterRegistry
    {
        private readonly IActionRouter[] _routers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionRouterRegistry"/> class.
        /// </summary>
        /// <param name="routers">
        /// The collection of available <see cref="IActionRouter"/> instances.
        /// If <c>null</c>, an empty collection will be used.
        /// </param>
        public ActionRouterRegistry(IEnumerable<IActionRouter> routers)
        {
            _routers = routers?.ToArray() ?? Array.Empty<IActionRouter>();
        }

        /// <inheritdoc />
        public bool TryResolve(string actionKey, out IActionRouter router)
        {
            var key = actionKey?.Trim().ToLowerInvariant() ?? string.Empty;
            router = _routers.FirstOrDefault(r => r.CanHandle(key));
            return router is not null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public sealed class ActionRouterRegistry : IActionRouterRegistry
    {
        private readonly IActionRouter[] _routers;

        public ActionRouterRegistry(IEnumerable<IActionRouter> routers)
        {
            _routers = routers?.ToArray() ?? Array.Empty<IActionRouter>();
        }

        public bool TryResolve(string actionKey, out IActionRouter router)
        {
            var key = actionKey?.Trim().ToLowerInvariant() ?? string.Empty;
            router = _routers.FirstOrDefault(r => r.CanHandle(key));
            return router is not null;
        }
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public sealed class ActionHandlerRegistry : IActionHandlerRegistry
    {
        private readonly ConcurrentDictionary<string, IActionHandler> _map;

        public ActionHandlerRegistry(IEnumerable<IActionHandler> handlers)
        {
            _map = new ConcurrentDictionary<string, IActionHandler>(
                handlers.ToDictionary(
                    h => h.Key.ToLowerInvariant().Trim(),
                    h => h));
        }

        public bool TryResolve(string key, out IActionHandler handler)
            => _map.TryGetValue(key.ToLowerInvariant().Trim(), out handler);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public interface IActionRouterRegistry
    {
        bool TryResolve(string actionKey, out IActionRouter router);
    }
}

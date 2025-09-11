using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    public interface IScenario
    {
        Task RunAsync();
        Task SetupAsync();
        Task ResetAsync();
    }
}

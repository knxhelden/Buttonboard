using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    public interface IScenario
    {
        Task RunAsync(CancellationToken ct = default);
        Task SetupAsync(CancellationToken ct = default);
        Task ResetAsync(CancellationToken ct = default);
    }
}

using BSolutions.Buttonboard.Services.Loaders;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public interface IActionHandler
    {
        string Key { get; }

        Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct);
    }
}

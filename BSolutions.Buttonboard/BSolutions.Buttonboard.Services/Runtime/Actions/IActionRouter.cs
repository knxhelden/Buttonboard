using BSolutions.Buttonboard.Services.Loaders;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public interface IActionRouter
    {
        string Domain { get; }

        bool CanHandle(string actionKey);

        Task ExecuteAsync(ScenarioAssetStep step, CancellationToken ct);
    }
}

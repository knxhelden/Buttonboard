using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public interface IActionExecutor
    {
        Task ExecuteAsync(SceneStep step, CancellationToken ct);
    }
}

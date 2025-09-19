using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtimes
{
    public interface ISceneRuntime
    {
        Task<bool> StartAsync(string sceneKey, CancellationToken ct = default);
        Task<bool> CancelAsync();
        bool IsRunning { get; }
        string? CurrentSceneKey { get; }
    }
}

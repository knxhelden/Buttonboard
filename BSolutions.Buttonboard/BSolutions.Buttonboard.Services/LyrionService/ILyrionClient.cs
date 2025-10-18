using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.LyrionService
{
    public interface ILyrionClient
    {
        Task<string> PlayUrlAsync(string playerName, string url, CancellationToken ct = default);
        Task<string> PauseAsync(string playerName, bool pause, CancellationToken ct = default);
        Task<string> SetVolumeAsync(string playerName, int volumePercent, CancellationToken ct = default);
    }
}

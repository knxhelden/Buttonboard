using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Integrations.Audio
{
    /// <summary>Plays files stored locally through the host's default sound card.</summary>
    public interface IAudioPlayer
    {
        Task PlayAsync(string file, string channel, int volume, bool loop, CancellationToken ct);
        Task StopAsync(string channel, CancellationToken ct);
        Task StopAllAsync(CancellationToken ct);
    }
}

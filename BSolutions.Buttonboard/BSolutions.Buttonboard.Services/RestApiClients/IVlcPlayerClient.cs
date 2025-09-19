using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public interface IVlcPlayerClient
    {
        Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player, CancellationToken ct = default);
    }
}

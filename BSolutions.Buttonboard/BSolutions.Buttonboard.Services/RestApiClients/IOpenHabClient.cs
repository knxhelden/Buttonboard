using BSolutions.Buttonboard.Services.Enumerations;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public interface IOpenHabClient
    {
        Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default);
        Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default);
        Task<string?> GetStateAsync(string itemname, CancellationToken ct = default);
        Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default);
    }
}

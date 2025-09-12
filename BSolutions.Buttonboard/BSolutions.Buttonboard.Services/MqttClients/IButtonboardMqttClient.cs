using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    public interface IButtonboardMqttClient
    {
        Task ConnectAsync();
        Task PublishAsync(string topic, string payload, CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);
    }
}

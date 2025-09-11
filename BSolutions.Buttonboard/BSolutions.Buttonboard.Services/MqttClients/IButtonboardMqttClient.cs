using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.MqttClients
{
    public interface IButtonboardMqttClient
    {
        Task ConnectAsync();
        Task PublishAsync(string topic, string payload);
    }
}

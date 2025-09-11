using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Enumerations;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public interface IOpenHabClient
    {
        Task<string> GetState(string itemname);
        Task SendCommandAsync(string itemname, OpenHabCommand command);
        Task SendCommandAsync(string itemname, string requestBody);
        void UpdateState(string itemname, OpenHabCommand command);
    }
}

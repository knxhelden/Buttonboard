using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Settings
{
    public interface ISettingsProvider
    {
        ApplicationOptions Application { get; }
        OpenHabOptions OpenHAB { get; }
        VlcOptions VLC { get; }
        MqttOptions Mqtt { get; }
    }
}

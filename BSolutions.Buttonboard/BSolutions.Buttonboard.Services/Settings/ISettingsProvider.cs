using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Settings
{
    public interface ISettingsProvider
    {
        Application Application { get; }

        Audio Audio { get; }

        OpenHAB OpenHAB { get; }

        Mqtt Mqtt { get; }

        VLC VLC { get; }
    }
}

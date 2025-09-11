using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario.Ghostbusters
{
    public class GhostbustersScenario : ScenarioBase
    {
        #region --- Constructor ---

        public GhostbustersScenario(ILogger<ScenarioBase> logger,
            ISettingsProvider settingsProvider,
            ButtonboardGpioController gpioController,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IButtonboardMqttClient mqtt)
            : base(logger, settingsProvider, gpioController, openhab, vlc, mqtt)
        {

        }

        #endregion

        protected override async Task RunScene1()
        {
            await this._mqtt.PublishAsync("cmnd/bremus/eg/buero/stehlampe/POWER", "ON");
        }

        protected override Task RunScene2()
        {
            return base.RunScene2();
        }

        protected override Task RunScene3()
        {
            return base.RunScene3();
        }

        protected override Task RunScene4()
        {
            return base.RunScene4();
        }

        public override Task ResetAsync()
        {
            return base.ResetAsync();
        }
    }
}

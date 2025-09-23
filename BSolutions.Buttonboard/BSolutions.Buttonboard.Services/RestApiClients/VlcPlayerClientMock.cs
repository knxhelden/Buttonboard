using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public sealed class VlcPlayerClientMock : IVlcPlayerClient
    {
        private readonly ILogger<VlcPlayerClientMock> _logger;
        private readonly IButtonboardGpioController _gpio;

        public VlcPlayerClientMock(ILogger<VlcPlayerClientMock> logger, IButtonboardGpioController gpio)
        {
            _logger = logger;
            _gpio = gpio;
        }

        public async Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player, CancellationToken ct = default)
        {
            if (player is null) throw new ArgumentNullException(nameof(player));

            await Task.Delay(50, ct).ConfigureAwait(false);

            _logger.LogInformation("[SIM] VLC command for player '{Player}': {Command}", player.Name, command);
        }
    }
}

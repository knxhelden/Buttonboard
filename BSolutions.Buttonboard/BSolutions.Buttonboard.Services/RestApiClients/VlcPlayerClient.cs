using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpioController;

        #region --- Constructor ---

        public VlcPlayerClient(ILogger<VlcPlayerClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpioController)
            : base(logger, settingsProvider)
        {
            this._gpioController = gpioController;
        }

        #endregion

        public async Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player)
        {
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $":{player.Password}".Base64Encode());
            string requestUri = $"{player.BaseUri}requests/status.xml?command={command.GetCommand()}";

            try
            {
                using (var response = await this._httpClient.GetAsync(requestUri))
                {
                    this._logger.LogDebug($"Send VLC Player command: {requestUri}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"The VLC request to uri '{requestUri}' was unsuccessful (Status Code: {response.StatusCode}).");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    this._logger.LogError(ex.InnerException, "An error occoured during a VLC Service Call.", requestUri);
                }
                else
                {
                    this._logger.LogError(ex, "An error occoured during a VLC Service Call.", requestUri);
                }

                // Error Led
                await this._gpioController.LedOnAsync(Led.SystemYellow);
            }
        }
    }
}

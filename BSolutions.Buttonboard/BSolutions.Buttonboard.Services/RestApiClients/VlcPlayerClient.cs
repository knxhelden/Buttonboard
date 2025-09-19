using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Device.Gpio;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        #region --- Constructor ---

        public VlcPlayerClient(ILogger<VlcPlayerClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            this._gpio = gpio;
        }

        #endregion

        public async Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player, CancellationToken ct = default)
        {
            if (player is null) throw new ArgumentNullException(nameof(player));
            if (player.BaseUri is null) throw new ArgumentException("VLC player BaseUri must be set.", nameof(player));

            // VLC HTTP auth: typically username is empty, password set -> format ":password"
            var basicToken = $":{player.Password}".Base64Encode();
            var requestUri = $"{player.BaseUri}requests/status.xml?command={command.GetCommand()}";

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                                                  .ConfigureAwait(false);

                _logger.LogDebug("Send VLC Player command: {Uri}", requestUri);

                if (!resp.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"The VLC request to uri '{requestUri}' was unsuccessful (Status Code: {resp.StatusCode}).");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("VLC request canceled: {Uri}", requestUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during a VLC service call: {Uri}", requestUri);
                await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false);
                throw;
            }
        }
    }
}

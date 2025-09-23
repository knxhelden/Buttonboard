using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public class OpenHabClient : RestApiClientBase, IOpenHabClient
    {
        private readonly IButtonboardGpioController _gpio;

        #region --- Constructor ---

        public OpenHabClient(ILogger<OpenHabClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpio)
            : base (logger, settingsProvider)
        {
            this._httpClient.BaseAddress = this._settings.OpenHAB.BaseUri;
            this._gpio = gpio;
        }

        #endregion

        #region --- IOpenHabClient ---

        /// <summary>
        /// Sends a command to an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The command to be sent to the item.</param>
        public Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
            => SendCommandAsync(itemname, command.ToString(), ct);

        /// <summary>
        /// Sends a command to an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The request body information to be sent to the item.</param>
        public async Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default)
        {
            var relativeUri = $"items/{itemname}";
            using var content = new StringContent(requestBody, Encoding.UTF8, "text/plain");

            _logger.LogDebug("URL: {Url} // Command: {Body}", relativeUri, requestBody);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, relativeUri)
                {
                    Content = content
                };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                                                      .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).";
                    throw new HttpRequestException(msg);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB request canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during an openHAB service call: {Url}", relativeUri);
                await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Gets the state of an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <returns>Returns the state of the item.</returns>
        public async Task<string?> GetStateAsync(string itemname, CancellationToken ct = default)
        {
            var relativeUri = $"items/{itemname}/state";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                                                      .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).";
                    throw new HttpRequestException(msg);
                }

                var state = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogDebug("openHAB state {Item}: {State}", itemname, state);
                return state;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB state request canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during an openHAB service call: {Url}", relativeUri);
                await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false);
                return default;
            }
        }

        /// <summary>
        /// Updates the state of an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The command to be sent to the item.</param>
        public async Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
        {
            var relativeUri = $"items/{itemname}/state";
            using var content = new StringContent(command.ToString(), Encoding.UTF8, "text/plain");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, relativeUri)
                {
                    Content = content
                };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                                                      .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).";
                    throw new HttpRequestException(msg);
                }

                _logger.LogDebug("openHAB state updated {Item}: {Command}", itemname, command);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB update canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during an openHAB service call: {Url}", relativeUri);
                await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false);
                throw;
            }
        }

        #endregion
    }
}

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
    /// <summary>
    /// HTTP client for interacting with the openHAB REST API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Base address is taken from <see cref="ISettingsProvider"/> (<c>OpenHAB.BaseUri</c>).
    /// Requests use <c>text/plain</c> for commands and states.
    /// </para>
    /// <para>
    /// On handled failures, the client logs the error and signals a system warning by
    /// switching on <see cref="Led.SystemYellow"/> via the GPIO controller.
    /// </para>
    /// </remarks>
    public class OpenHabClient : RestApiClientBase, IOpenHabClient
    {
        private readonly IButtonboardGpioController _gpio;

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="OpenHabClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error reporting.</param>
        /// <param name="settingsProvider">Provides <c>OpenHAB.BaseUri</c> for the underlying <see cref="_httpClient"/>.</param>
        /// <param name="gpio">GPIO controller used to signal failures via LEDs.</param>
        public OpenHabClient(ILogger<OpenHabClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpio)
            : base (logger, settingsProvider)
        {
            this._httpClient.BaseAddress = this._settings.OpenHAB.BaseUri;
            this._gpio = gpio;
        }

        #endregion

        #region --- IOpenHabClient ---

        /// <summary>
        /// Sends a command to an item (POST <c>/items/{itemname}</c>), using a strongly-typed command.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The command to be sent to the item.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
            => SendCommandAsync(itemname, command.ToString(), ct);

        /// <summary>
        /// Sends a command to an item (POST <c>/items/{itemname}</c>), using a raw plain-text body.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="requestBody">The request body to be sent as <c>text/plain</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <exception cref="HttpRequestException">Thrown on non-success HTTP status codes.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
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
        /// Gets the current state of an item (GET <c>/items/{itemname}/state</c>).
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// The state string on success; <c>null</c> if an exception was handled and signaled.
        /// </returns>
        /// <exception cref="HttpRequestException">Thrown on non-success HTTP status codes.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
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
        /// Updates the state of an item (PUT <c>/items/{itemname}/state</c>).
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The state to PUT as plain text.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <exception cref="HttpRequestException">Thrown on non-success HTTP status codes.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
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

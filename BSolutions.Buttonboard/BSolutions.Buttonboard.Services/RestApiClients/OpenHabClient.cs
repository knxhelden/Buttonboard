using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public class OpenHabClient : RestApiClientBase, IOpenHabClient
    {
        private readonly IButtonboardGpioController _gpioController;

        #region --- Constructor ---

        public OpenHabClient(ILogger<OpenHabClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpioController)
            : base (logger, settingsProvider)
        {
            this._httpClient.BaseAddress = this._settings.OpenHAB.BaseUri;
            this._gpioController = gpioController;
        }

        #endregion

        #region --- IOpenHabClient ---

        /// <summary>
        /// Sends a command to an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The command to be sent to the item.</param>
        public async Task SendCommandAsync(string itemname, OpenHabCommand command)
        {
            await this.SendCommandAsync(itemname, command.ToString());
        }

        /// <summary>
        /// Sends a command to an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The request body information to be sent to the item.</param>
        public async Task SendCommandAsync(string itemname, string requestBody)
        {
            string relativeUri = $"items/{itemname}";
            StringContent content = new StringContent(requestBody);
            this._logger.LogDebug($"URL: {relativeUri.ToString()} // Command: {requestBody}");

            try
            {
                this._httpClient.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using (var response = await this._httpClient.PostAsync(relativeUri, content))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).");
                    }

                    //this._logger.LogDebug(response.ToString());
                }
            }
            catch (Exception ex)
            {
                if(ex.InnerException != null)
                {
                    this._logger.LogError(ex.InnerException, "An error occoured during a openHAB Service Call.", relativeUri);
                }
                else
                {
                    this._logger.LogError(ex, "An error occoured during a openHAB Service Call.", relativeUri);
                }

                // Error Led
                await this._gpioController.LedOnAsync(Led.SystemYellow);
            }
        }

        /// <summary>
        /// Gets the state of an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <returns>Returns the state of the item.</returns>
        public async Task<string> GetState(string itemname)
        {
            string relativeUri = $"items/{itemname}/state";

            try
            {
                using (var response = await this._httpClient.GetAsync(relativeUri))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).");
                    }

                    this._logger.LogDebug(response.ToString());
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "An error occoured during a openHAB Service Call.", relativeUri);

                // Error Led
                await this._gpioController.LedOnAsync(Led.SystemYellow);

                return default;
            }
        }

        /// <summary>
        /// Updates the state of an item.
        /// </summary>
        /// <param name="itemname">The item name.</param>
        /// <param name="command">The command to be sent to the item.</param>
        public async void UpdateState(string itemname, OpenHabCommand command)
        {
            string relativeUri = $"items/{itemname}/state";
            StringContent content = new StringContent(command.ToString());

            try
            {
                this._httpClient.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using (var response = await this._httpClient.PutAsync(relativeUri, content))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"The openHAB request to uri '{relativeUri}' was unsuccessful (Status Code: {response.StatusCode}).");
                    }

                    this._logger.LogDebug(response.ToString());
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "An error occoured during a openHAB Service Call.", relativeUri);

                // Error Led
                await this._gpioController.LedOnAsync(Led.SystemYellow);
            }
        }

        #endregion
    }
}

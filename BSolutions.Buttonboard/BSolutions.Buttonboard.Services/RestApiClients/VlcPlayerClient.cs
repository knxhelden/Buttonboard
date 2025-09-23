using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// HTTP client for issuing remote control commands to a VLC instance via its
    /// built-in HTTP interface (<c>/requests/status.xml</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// VLC's legacy HTTP interface typically uses Basic authentication where the
    /// username is empty and only a password is configured. The corresponding
    /// <c>Authorization</c> header therefore contains the Base64-encoded string
    /// <c>":&lt;password&gt;"</c>.
    /// </para>
    /// <para>
    /// This client constructs a <c>GET</c> request to
    /// <c>requests/status.xml?command=&lt;cmd&gt;</c>, where <paramref name="command"/>
    /// is transformed to VLC's query parameter using <see cref="VlcPlayerCommandExtensions.GetCommand"/>.
    /// </para>
    /// <para>
    /// On non-success HTTP status codes, the method throws an <see cref="HttpRequestException"/>.
    /// Any unexpected exception leads to logging an error and switching on the
    /// <see cref="Led.SystemYellow"/> to signal a system issue.
    /// </para>
    /// </remarks>
    /// <seealso cref="IVlcPlayerClient"/>
    public class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="VlcPlayerClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error reporting.</param>
        /// <param name="settingsProvider">Provides configuration used by the base REST client.</param>
        /// <param name="gpio">GPIO controller used to signal failures via LEDs.</param>
        public VlcPlayerClient(ILogger<VlcPlayerClient> logger, ISettingsProvider settingsProvider, IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            this._gpio = gpio;
        }

        #endregion

        /// <summary>
        /// Sends a control <paramref name="command"/> to the specified VLC <paramref name="player"/>.
        /// </summary>
        /// <param name="command">The VLC command to execute (e.g., play, pause, stop).</param>
        /// <param name="player">The target VLC player configuration (base URI and password).</param>
        /// <param name="ct">An optional cancellation token to cancel the request.</param>
        /// <returns>A task that completes when the command has been sent and the response received.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="player"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <see cref="VLCPlayer.BaseUri"/> is not set on <paramref name="player"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the operation is canceled via <paramref name="ct"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// Thrown when VLC responds with a non-success HTTP status code.
        /// </exception>
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

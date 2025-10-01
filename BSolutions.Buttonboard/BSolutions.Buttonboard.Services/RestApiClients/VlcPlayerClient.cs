using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Logging;
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
    /// VLC's legacy HTTP interface usually uses Basic auth with empty username and a password.
    /// The Authorization header contains Base64(":<password>").
    /// </remarks>
    public class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Creates a new <see cref="VlcPlayerClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error reporting.</param>
        /// <param name="settingsProvider">Provides configuration used by the base REST client.</param>
        /// <param name="gpio">GPIO controller used to signal failures via LEDs.</param>
        public VlcPlayerClient(
            ILogger<RestApiClientBase> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        /// <inheritdoc />
        public async Task SendCommandAsync(VlcPlayerCommand command, VLCPlayer player, CancellationToken ct = default)
        {
            if (player is null) throw new ArgumentNullException(nameof(player));
            if (player.BaseUri is null) throw new ArgumentException("VLC player BaseUri must be set.", nameof(player));

            // Build auth + request
            var basicToken = $":{player.Password}".Base64Encode(); // do not log secrets
            var requestUri = $"{player.BaseUri}requests/status.xml?command={command.GetCommand()}";

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC command sent Player {Player} Command {Command} Url {Url}",
                    player.Name ?? "(unnamed)", command, requestUri);

                using var resp = await _httpClient
                    .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(LogEvents.VlcNonSuccess,
                        "VLC non-success Player {Player} Command {Command} -> {StatusCode}",
                        player.Name ?? "(unnamed)", command, (int)resp.StatusCode);

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    resp.EnsureSuccessStatusCode(); // throws HttpRequestException
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("VLC request canceled: {Url}", requestUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.VlcError, ex,
                    "VLC request failed Player {Player} Command {Command} Url {Url}",
                    player.Name ?? "(unnamed)", command, requestUri);

                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }
        }
    }
}
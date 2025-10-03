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
            if (string.IsNullOrWhiteSpace(player.BaseUri))
                throw new ArgumentException("VLC player BaseUri must be set.", nameof(player));

            // --- Build a robust target URI ---
            // 1) Ensure base (with slash)
            var baseUriText = player.BaseUri.EndsWith("/") ? player.BaseUri : player.BaseUri + "/";
            var baseUri = new Uri(baseUriText, UriKind.Absolute);

            // 2) Path + query with encoding
            var endpoint = new Uri(baseUri, "requests/status.xml");
            var cmdText = command.GetCommand();
            var finalUri = new Uri($"{endpoint}?command={Uri.EscapeDataString(cmdText)}");

            // --- Build Basic Auth (empty/null -> empty string) ---
            var password = player.Password ?? string.Empty;
            var basicToken = $":{password}".Base64Encode();

            // --- Per-Request Timeout (in addition to ct) ---
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, finalUri);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC command → Player {Player} Command {Command} Url {Url}",
                    player.Name ?? "(unnamed)", command, finalUri);

                using var resp = await _httpClient
                    .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                    .ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    var code = (int)resp.StatusCode;
                    if (code == 401 || code == 403)
                    {
                        _logger.LogWarning(LogEvents.VlcNonSuccess,
                            "VLC auth failed Player {Player} Command {Command} -> {StatusCode}",
                            player.Name ?? "(unnamed)", command, code);
                    }
                    else
                    {
                        _logger.LogWarning(LogEvents.VlcNonSuccess,
                            "VLC non-success Player {Player} Command {Command} -> {StatusCode}",
                            player.Name ?? "(unnamed)", command, code);
                    }

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    resp.EnsureSuccessStatusCode();
                }
            }
            catch (OperationCanceledException)
            {
                // Comes either from ct (scene abort) or from per-request timeout
                _logger.LogInformation("VLC request canceled/timeout: {Url}", finalUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.VlcError, ex,
                    "VLC request failed Player {Player} Command {Command} Url {Url}",
                    player.Name ?? "(unnamed)", command, finalUri);

                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }
        }

    }
}
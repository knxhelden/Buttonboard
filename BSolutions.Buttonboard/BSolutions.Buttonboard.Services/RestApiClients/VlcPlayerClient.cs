using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// HTTP client for issuing remote control commands to a VLC instance via its legacy HTTP interface.
    /// </summary>
    public class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        public VlcPlayerClient(
            ILogger<RestApiClientBase> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        /// <inheritdoc />
        public async Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name must be provided.", nameof(playerName));

            var players = _settings.VLC?.Entries
                ?? throw new InvalidOperationException("No VLC players configured.");

            if (!players.TryGetValue(playerName, out var player) || player is null)
                throw new KeyNotFoundException($"VLC player '{playerName}' not configured.");

            var baseUri = player.BaseUri
                ?? throw new InvalidOperationException($"VLC player '{playerName}' has no BaseUri configured.");

            if (!baseUri.IsAbsoluteUri)
                throw new InvalidOperationException($"VLC player '{playerName}' BaseUri must be absolute: '{baseUri}'.");

            // Build endpoint and final command URL (xml endpoint is fine for commands)
            var endpoint = new Uri(baseUri, "requests/status.xml");
            var cmdText = command.GetCommand();
            var finalUri = new Uri($"{endpoint}?command={Uri.EscapeDataString(cmdText)}");

            // Basic-Auth (VLC expects empty username and password in HTTP Basic)
            var password = player.Password ?? string.Empty;
            var basicToken = $":{password}".Base64Encode();

            // Per-request timeout (bounded)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, finalUri);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC command → Player {Player} Command {Command} Url {Url}",
                    playerName, command, finalUri);

                using var resp = await _httpClient
                    .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                    .ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    var code = (int)resp.StatusCode;
                    if (code is 401 or 403)
                    {
                        _logger.LogWarning(LogEvents.VlcNonSuccess,
                            "VLC auth failed Player {Player} Command {Command} -> {StatusCode}",
                            playerName, command, code);
                    }
                    else
                    {
                        _logger.LogWarning(LogEvents.VlcNonSuccess,
                            "VLC non-success Player {Player} Command {Command} -> {StatusCode}",
                            playerName, command, code);
                    }

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    resp.EnsureSuccessStatusCode();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("VLC request canceled/timeout: {Url}", finalUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.VlcError, ex,
                    "VLC request failed Player {Player} Command {Command} Url {Url}",
                    playerName, command, finalUri);

                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }
        }
    }
}
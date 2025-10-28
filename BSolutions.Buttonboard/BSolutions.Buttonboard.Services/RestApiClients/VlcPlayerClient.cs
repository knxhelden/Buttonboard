using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// HTTP client for issuing remote control commands to VLC instances via their legacy HTTP interface.
    /// </summary>
    /// <remarks>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item><description>Send high-level control commands to configured VLC players.</description></item>
    ///   <item><description>Perform bulk resets of all players to a consistent paused state.</description></item>
    ///   <item><description>Ensure robust timeout handling and structured logging for diagnostics.</description></item>
    /// </list>
    /// Thread-safety: instances are stateless per call and intended for transient use.
    /// </remarks>
    public sealed class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Creates a new <see cref="VlcPlayerClient"/> using the shared HTTP infrastructure.
        /// </summary>
        /// <param name="logger">Logger for structured diagnostics.</param>
        /// <param name="settingsProvider">Provides access to global application settings.</param>
        /// <param name="gpio">Used to signal hardware status LEDs on warnings.</param>
        public VlcPlayerClient(
            ILogger<RestApiClientBase> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        /// <inheritdoc />
        public async Task ResetAsync(CancellationToken ct = default)
        {
            var vlc = _settings.VLC ?? throw new InvalidOperationException("VLC settings missing.");

            if (vlc.Devices is null || vlc.Devices.Count == 0)
            {
                _logger.LogInformation("VLC reset: no players configured.");
                return;
            }

            var resetCount = 0;

            _logger.LogInformation("Starting reset of {Count} VLC player(s)…", vlc.Devices.Count);

            foreach (var kvp in vlc.Devices)
            {
                ct.ThrowIfCancellationRequested();

                var playerName = kvp.Key;
                var player = kvp.Value;

                if (player?.BaseUri is null)
                {
                    _logger.LogWarning("VLC reset: skipping player '{Player}' – no BaseUri configured.", playerName);
                    continue;
                }

                if (!player.BaseUri.IsAbsoluteUri)
                {
                    _logger.LogWarning("VLC reset: skipping player '{Player}' – invalid BaseUri '{Uri}'.", playerName, player.BaseUri);
                    continue;
                }

                try
                {
                    await ResetPlayerAsync(playerName, player.BaseUri, player.Password, ct).ConfigureAwait(false);
                    resetCount++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Best-effort: other players continue
                    _logger.LogWarning(ex, "VLC reset: failed for player '{Player}'.", playerName);
                }
            }

            _logger.LogInformation("VLC reset complete. Players reset successfully: {Count}/{Total}", resetCount, vlc.Devices.Count);
        }

        /// <summary>
        /// Performs the actual reset logic for a single VLC instance:
        /// loads the playlist, plays the last item, seeks near its end, then pauses.
        /// </summary>
        private async Task ResetPlayerAsync(string playerName, Uri baseUri, string? password, CancellationToken ct)
        {
            var httpRoot = new Uri(baseUri, "requests/");
            var basicToken = $":{password ?? string.Empty}".Base64Encode();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            // 1) Read playlist
            var playlistUri = new Uri(httpRoot, "playlist.xml");
            using var playlistReq = new HttpRequestMessage(HttpMethod.Get, playlistUri);
            playlistReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
            using var playlistResp = await _httpClient.SendAsync(playlistReq, timeoutCts.Token).ConfigureAwait(false);
            playlistResp.EnsureSuccessStatusCode();

            var xdoc = XDocument.Load(await playlistResp.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false));

            // Find the playlist node (EN/DE) or first node with leaves
            var playlistNode =
                xdoc.Descendants("node")
                   .FirstOrDefault(n =>
                        string.Equals((string)n.Attribute("type"), "playlist", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals((string)n.Attribute("name"), "Playlist", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals((string)n.Attribute("name"), "Wiedergabeliste", StringComparison.OrdinalIgnoreCase))
                ?? xdoc.Descendants("node").FirstOrDefault(n => n.Elements("leaf").Any())
                ?? xdoc.Root;

            // Last leaf by document order  ← THIS was the change you needed
            var lastLeaf = playlistNode?.Elements("leaf").LastOrDefault();
            if (lastLeaf is null)
            {
                _logger.LogWarning(LogEvents.VlcNonSuccess,
                    "VLC Reset → No playlist entry found for player {Player}", playerName);
                return;
            }

            var lastId = (string?)lastLeaf.Attribute("id");
            var lastName = (string?)lastLeaf.Attribute("name");
            if (string.IsNullOrWhiteSpace(lastId))
            {
                _logger.LogWarning(LogEvents.VlcNonSuccess,
                    "VLC Reset → Found leaf without id for player {Player}", playerName);
                return;
            }

            _logger.LogInformation(LogEvents.VlcCommandSent,
                "VLC Reset → Player {Player}: selecting LAST-BY-ORDER id={Id} name=\"{Name}\"",
                playerName, lastId, lastName);

            // 2) Play that item  ← revert to in_play (this worked in your setup)
            var playUri = new Uri(httpRoot, $"status.xml?command=pl_play&id={Uri.EscapeDataString(lastId)}");
            await ExecuteSimpleGetAsync(playUri, basicToken, timeoutCts.Token).ConfigureAwait(false);

            // tiny wait so 'length' is populated in status.xml (this was your original working pattern)
            await Task.Delay(500, timeoutCts.Token).ConfigureAwait(false);

            // 3) Read length from status.xml
            int? lengthSec = null;
            var statusUri = new Uri(httpRoot, "status.xml");
            using (var req = new HttpRequestMessage(HttpMethod.Get, statusUri))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
                using var resp = await _httpClient.SendAsync(req, timeoutCts.Token).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                var statusXml = XDocument.Load(await resp.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false));
                if (int.TryParse(statusXml.Root?.Element("length")?.Value, out var len))
                    lengthSec = len;
            }

            // 4) Absolute seek to (length - 5) seconds (no unit → absolute seconds)
            if (lengthSec is int L && L > 5)
            {
                var target = Math.Max(0, L - 5);
                var seekUri = new Uri(httpRoot, $"status.xml?command=seek&val={target}");
                await ExecuteSimpleGetAsync(seekUri, basicToken, timeoutCts.Token).ConfigureAwait(false);
            }

            // 5) Force pause
            var pauseUri = new Uri(httpRoot, "status.xml?command=pl_forcepause");
            await ExecuteSimpleGetAsync(pauseUri, basicToken, timeoutCts.Token).ConfigureAwait(false);

            _logger.LogInformation(LogEvents.VlcCommandSent,
                "VLC player {Player} reset: last item queued at tail and paused", playerName);
        }


        /// <summary>
        /// Executes a simple authorized GET request against a VLC endpoint.
        /// </summary>
        private async Task ExecuteSimpleGetAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        /// <inheritdoc />
        public async Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name must be provided.", nameof(playerName));

            var players = _settings.VLC?.Devices
                ?? throw new InvalidOperationException("No VLC players configured.");

            if (!players.TryGetValue(playerName, out var player) || player is null)
                throw new KeyNotFoundException($"VLC player '{playerName}' not configured.");

            var baseUri = player.BaseUri
                ?? throw new InvalidOperationException($"VLC player '{playerName}' has no BaseUri configured.");

            var endpoint = new Uri(baseUri, "requests/status.xml");
            var cmdText = command.GetCommand();
            var finalUri = new Uri($"{endpoint}?command={Uri.EscapeDataString(cmdText)}");
            var password = player.Password ?? string.Empty;
            var basicToken = $":{password}".Base64Encode();

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
                    _logger.LogWarning(LogEvents.VlcNonSuccess,
                        "VLC non-success Player {Player} Command {Command} -> {Code}",
                        playerName, command, (int)resp.StatusCode);
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
                throw;
            }
        }
    }
}

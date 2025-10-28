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
    /// Real HTTP-based implementation of <see cref="IVlcPlayerClient"/> that communicates
    /// with VLC instances via their legacy <c>/requests/</c> HTTP interface.
    /// </summary>
    /// <remarks>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item><description>Send high-level playback and control commands to VLC players.</description></item>
    ///   <item><description>Perform bulk resets of all configured players to a defined idle state.</description></item>
    ///   <item><description>Handle network timeouts and structured logging for diagnostics.</description></item>
    /// </list>
    /// Thread-safety: this class is <b>stateless per call</b> and intended for transient use.
    /// </remarks>
    public sealed class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Centralized route definitions for the VLC HTTP interface.
        /// </summary>
        private static class Routes
        {
            public const string Requests = "requests/";
            public const string Status = "status.xml";
            public const string Playlist = "playlist.xml";
        }

        #region --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="VlcPlayerClient"/> class.
        /// </summary>
        /// <param name="logger">Logger for structured diagnostics.</param>
        /// <param name="settingsProvider">Provides global application configuration.</param>
        /// <param name="gpio">Used to signal hardware status (e.g., LED warnings).</param>
        public VlcPlayerClient(
            ILogger<RestApiClientBase> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio)
            : base(logger, settingsProvider)
        {
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        #endregion

        #region --- IVlcPlayerClient ---

        /// <inheritdoc />
        public async Task ResetAsync(CancellationToken ct = default)
        {
            var cfg = _settings.VLC ?? throw new InvalidOperationException("VLC settings missing.");

            if (cfg.Devices is null || cfg.Devices.Count == 0)
            {
                _logger.LogInformation("VLC Reset: no players configured.");
                return;
            }

            _logger.LogInformation("VLC Reset: starting for {Count} player(s)…", cfg.Devices.Count);

            var ok = 0;
            foreach (var (playerName, device) in cfg.Devices)
            {
                ct.ThrowIfCancellationRequested();

                if (device?.BaseUri is null || !device.BaseUri.IsAbsoluteUri)
                {
                    _logger.LogWarning("VLC Reset: skip '{Player}' – invalid BaseUri.", playerName);
                    continue;
                }

                try
                {
                    await ResetPlayerAsync(playerName, device.BaseUri, device.Password, ct).ConfigureAwait(false);
                    ok++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "VLC Reset: failed for '{Player}'.", playerName);
                }
            }

            _logger.LogInformation("VLC Reset: finished. Successful: {Ok}/{Total}", ok, cfg.Devices.Count);
        }

        /// <inheritdoc />
        public async Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name must be provided.", nameof(playerName));

            var players = _settings.VLC?.Devices
                ?? throw new InvalidOperationException("No VLC players configured.");

            if (!players.TryGetValue(playerName, out var player) || player?.BaseUri is null)
                throw new KeyNotFoundException($"VLC player '{playerName}' not configured or missing BaseUri.");

            var endpoint = new Uri(new Uri(player.BaseUri, Routes.Requests), Routes.Status);
            var finalUri = new Uri($"{endpoint}?command={Uri.EscapeDataString(command.GetCommand())}");
            var auth = CreateBasicToken(player.Password);

            using var to = CancellationTokenSource.CreateLinkedTokenSource(ct);
            to.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, finalUri);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC → Player {Player} Command {Command} Url {Url}",
                    playerName, command, finalUri);

                using var resp = await _httpClient
                    .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, to.Token)
                    .ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(LogEvents.VlcNonSuccess,
                        "VLC non-success → Player {Player} Command {Command} -> {Code}",
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
                    "VLC request failed → Player {Player} Command {Command} Url {Url}",
                    playerName, command, finalUri);
                throw;
            }
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Executes the full reset sequence for a single VLC instance:
        /// load playlist → select last item → short delay → read duration → seek near end → pause.
        /// </summary>
        private async Task ResetPlayerAsync(string playerName, Uri baseUri, string? password, CancellationToken ct)
        {
            var requestsRoot = new Uri(baseUri, Routes.Requests);
            var statusUri = new Uri(requestsRoot, Routes.Status);
            var playlistUri = new Uri(requestsRoot, Routes.Playlist);
            var authToken = CreateBasicToken(password);

            using var to = CancellationTokenSource.CreateLinkedTokenSource(ct);
            to.CancelAfter(TimeSpan.FromSeconds(5));

            // 1) Load playlist
            var xdoc = await GetXmlAsync(playlistUri, authToken, to.Token).ConfigureAwait(false);
            var lastItem = TryGetLastPlaylistItem(xdoc);

            if (lastItem is null)
            {
                _logger.LogWarning(LogEvents.VlcNonSuccess,
                    "VLC Reset → {Player}: no playlist entries.", playerName);
                return;
            }

            var (lastId, lastName) = lastItem.Value;
            _logger.LogInformation(LogEvents.VlcCommandSent,
                "VLC Reset → {Player}: selecting last playlist item id={Id} name=\"{Name}\"",
                playerName, lastId, lastName);

            // 2) Play the item
            var playUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_play&id={Uri.EscapeDataString(lastId)}");
            await SimpleGetAsync(playUri, authToken, to.Token).ConfigureAwait(false);

            // 3) Wait for status.xml to populate "length"
            await Task.Delay(500, to.Token).ConfigureAwait(false);

            // 4) Read total length
            var statusXml = await GetXmlAsync(statusUri, authToken, to.Token).ConfigureAwait(false);
            var lengthSec = ParseLengthInSeconds(statusXml);

            // 5) Seek to last 5 seconds
            if (lengthSec is int L && L > 5)
            {
                var target = Math.Max(0, L - 5);
                var seekUri = new Uri(requestsRoot, $"{Routes.Status}?command=seek&val={target}");
                await SimpleGetAsync(seekUri, authToken, to.Token).ConfigureAwait(false);
            }

            // 6) Force pause
            var pauseUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_forcepause");
            await SimpleGetAsync(pauseUri, authToken, to.Token).ConfigureAwait(false);

            _logger.LogInformation(LogEvents.VlcCommandSent,
                "VLC Reset → {Player}: item selected, seeked near end, paused.", playerName);
        }

        /// <summary>
        /// Creates a Basic Authentication header value for VLC requests.
        /// </summary>
        private static string CreateBasicToken(string? password)
            => $":{password ?? string.Empty}".Base64Encode();

        /// <summary>
        /// Executes an authorized GET request and parses the response as XML.
        /// </summary>
        private async Task<XDocument> GetXmlAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return XDocument.Load(stream);
        }

        /// <summary>
        /// Executes a simple authorized GET request, ensuring a successful HTTP status.
        /// </summary>
        private async Task SimpleGetAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Attempts to retrieve the last playlist item (by document order) from the given VLC playlist XML.
        /// </summary>
        private static (string id, string? name)? TryGetLastPlaylistItem(XDocument playlistXml)
        {
            var playlistNode =
                playlistXml.Descendants("node").FirstOrDefault(n =>
                    string.Equals((string)n.Attribute("type"), "playlist", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((string)n.Attribute("name"), "Playlist", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((string)n.Attribute("name"), "Wiedergabeliste", StringComparison.OrdinalIgnoreCase))
                ?? playlistXml.Descendants("node").FirstOrDefault(n => n.Elements("leaf").Any())
                ?? playlistXml.Root;

            var lastLeaf = playlistNode?.Elements("leaf").LastOrDefault();
            if (lastLeaf is null) return null;

            var id = (string?)lastLeaf.Attribute("id");
            if (string.IsNullOrWhiteSpace(id)) return null;

            var name = (string?)lastLeaf.Attribute("name");
            return (id, name);
        }

        /// <summary>
        /// Extracts the total media length (in seconds) from a VLC <c>status.xml</c> document.
        /// </summary>
        private static int? ParseLengthInSeconds(XDocument statusXml)
            => int.TryParse(statusXml.Root?.Element("length")?.Value, out var len) ? len : null;

        #endregion
    }
}

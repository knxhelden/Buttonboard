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
    /// HTTP-based implementation of <see cref="IVlcPlayerClient"/> that talks to VLC via the legacy <c>/requests/</c> API.
    /// </summary>
    /// <remarks>
    /// Sends playback/control commands, performs bulk resets, and applies short network timeouts.
    /// </remarks>
    public sealed class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Centralized route fragments for the VLC HTTP interface.
        /// </summary>
        private static class Routes
        {
            public const string Requests = "requests/";
            public const string Status = "status.xml";
            public const string Playlist = "playlist.xml";
        }

        #region --- Constructor ---

        /// <summary>
        /// Creates a new <see cref="VlcPlayerClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="settingsProvider">Application settings (incl. VLC devices).</param>
        /// <param name="gpio">GPIO controller for hardware signals (e.g., LED warnings).</param>
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

        /// <summary>
        /// Sends a single VLC command to the specified player.
        /// </summary>
        /// <param name="command">Logical command to execute (e.g., <see cref="VlcPlayerCommand.PAUSE"/>).</param>
        /// <param name="playerName">Configured VLC player name (key from settings).</param>
        /// <param name="ct">Cancellation token.</param>
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

        /// <summary>
        /// Plays the playlist entry at the given 1-based position.
        /// </summary>
        /// <param name="playerName">Configured VLC player name (key from settings).</param>
        /// <param name="position1Based">1-based playlist position (1 = first item).</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task PlayPlaylistItemAtAsync(string playerName, int position1Based, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name must be provided.", nameof(playerName));
            if (position1Based < 1)
                throw new ArgumentOutOfRangeException(nameof(position1Based), "Position must be >= 1.");

            var players = _settings.VLC?.Devices
                ?? throw new InvalidOperationException("No VLC players configured.");

            if (!players.TryGetValue(playerName, out var player) || player?.BaseUri is null || !player.BaseUri.IsAbsoluteUri)
                throw new KeyNotFoundException($"VLC player '{playerName}' not configured or missing/invalid BaseUri.");

            var requestsRoot = new Uri(player.BaseUri, Routes.Requests);
            var playlistUri = new Uri(requestsRoot, Routes.Playlist);
            var authToken = CreateBasicToken(player.Password);

            using var to = CancellationTokenSource.CreateLinkedTokenSource(ct);
            to.CancelAfter(TimeSpan.FromSeconds(5));

            // Load playlist and resolve the desired entry
            var xdoc = await GetXmlAsync(playlistUri, authToken, to.Token).ConfigureAwait(false);

            var item = TryGetPlaylistItemByIndex(xdoc, position1Based);
            if (item is null)
                throw new ArgumentOutOfRangeException(nameof(position1Based), $"Playlist does not contain position {position1Based}.");

            var (id, name) = item.Value;

            _logger.LogInformation(LogEvents.VlcCommandSent,
                "VLC → Player {Player}: playing playlist position {Pos} (id={Id}, name=\"{Name}\")",
                playerName, position1Based, id, name);

            // Play via pl_play&id
            var playUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_play&id={Uri.EscapeDataString(id)}");
            await SimpleGetAsync(playUri, authToken, to.Token).ConfigureAwait(false);
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Resets a single VLC instance: select last item → short delay → read duration → seek near end → force pause.
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

            // 2) Play last item
            var playUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_play&id={Uri.EscapeDataString(lastId)}");
            await SimpleGetAsync(playUri, authToken, to.Token).ConfigureAwait(false);

            // 3) Wait for status.xml to expose "length"
            await Task.Delay(500, to.Token).ConfigureAwait(false);

            // 4) Read total length
            var statusXml = await GetXmlAsync(statusUri, authToken, to.Token).ConfigureAwait(false);
            var lengthSec = ParseLengthInSeconds(statusXml);

            // 5) Seek to last 5 seconds (if known)
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
        /// Builds the Basic auth token (username is empty, password optional).
        /// </summary>
        private static string CreateBasicToken(string? password)
            => $":{password ?? string.Empty}".Base64Encode();

        /// <summary>
        /// Executes an authorized GET and parses the body as XML.
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
        /// Executes an authorized GET and ensures a successful status code.
        /// </summary>
        private async Task SimpleGetAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Returns the last playlist item (document order) from a VLC playlist XML.
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
        /// Reads the total media length (seconds) from a VLC <c>status.xml</c> document.
        /// </summary>
        private static int? ParseLengthInSeconds(XDocument statusXml)
            => int.TryParse(statusXml.Root?.Element("length")?.Value, out var len) ? len : null;

        /// <summary>
        /// Returns the playlist item at the given 1-based position (flattens nested folders).
        /// </summary>
        private static (string id, string? name)? TryGetPlaylistItemByIndex(XDocument playlistXml, int position1Based)
        {
            if (playlistXml?.Root is null) return null;

            var playlistNode =
                playlistXml.Descendants("node").FirstOrDefault(n =>
                    string.Equals((string)n.Attribute("type"), "playlist", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((string)n.Attribute("name"), "Playlist", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((string)n.Attribute("name"), "Wiedergabeliste", StringComparison.OrdinalIgnoreCase))
                ?? playlistXml.Root;

            var leaves = playlistNode
                .Descendants("leaf")
                .ToList();

            if (leaves.Count == 0) return null;

            var idx = position1Based - 1; // 1-based → 0-based
            if (idx < 0 || idx >= leaves.Count) return null;

            var leaf = leaves[idx];
            var id = (string?)leaf.Attribute("id");
            if (string.IsNullOrWhiteSpace(id)) return null;

            var name = (string?)leaf.Attribute("name");
            return (id!, name);
        }

        #endregion
    }
}

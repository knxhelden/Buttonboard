using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// HTTP-based implementation of <see cref="IVlcPlayerClient"/> that talks to VLC via the legacy <c>/requests/</c> API.
    /// </summary>
    /// <remarks>
    /// Adds per-host request serialization, simple retries with backoff, and small settle delays
    /// to increase robustness under concurrent scene execution.
    /// </remarks>
    public sealed class VlcPlayerClient : RestApiClientBase, IVlcPlayerClient
    {
        private readonly IButtonboardGpioController _gpio;

        private static class Routes
        {
            public const string Requests = "requests/";
            public const string Status = "status.xml";
            public const string Playlist = "playlist.xml";
        }

        // ── Concurrency guard: prevent concurrent calls to the same VLC host ─────────
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _hostLocks = new();
        private static SemaphoreSlim GetHostLock(Uri baseUri)
            => _hostLocks.GetOrAdd(baseUri.Host, _ => new SemaphoreSlim(1, 1));

        // ── Simple retry with exponential backoff for transient I/O issues ───────────
        private static async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> action,
            int maxAttempts,
            TimeSpan[]? backoff,
            CancellationToken ct)
        {
            backoff ??= new[]
            {
                TimeSpan.FromMilliseconds(150),
                TimeSpan.FromMilliseconds(350),
                TimeSpan.FromMilliseconds(750)
            };

            Exception? last = null;
            for (var attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { return await action(ct).ConfigureAwait(false); }
                catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
                {
                    last = ex;
                    var delay = backoff[Math.Min(attempt - 1, backoff.Length - 1)];
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (Exception ex) { last = ex; break; }
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(last!).Throw();
            throw last!;
        }

        private static Task ExecuteWithRetryAsync(
            Func<CancellationToken, Task> action,
            int maxAttempts,
            TimeSpan[]? backoff,
            CancellationToken ct)
            => ExecuteWithRetryAsync<object>(async t => { await action(t).ConfigureAwait(false); return default!; },
                                             maxAttempts, backoff, ct);

        private static bool IsTransient(Exception ex)
            => ex is HttpRequestException
            || ex is IOException
            || ex is SocketException
            || ex is TaskCanceledException;

        #region --- Constructor ---

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
            to.CancelAfter(TimeSpan.FromSeconds(8));

            var hostLock = GetHostLock(player.BaseUri);
            await hostLock.WaitAsync(to.Token).ConfigureAwait(false);
            try
            {
                await ExecuteWithRetryAsync(async t =>
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, finalUri);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

                    _logger.LogInformation(LogEvents.VlcCommandSent,
                        "VLC → Player {Player} Command {Command} Url {Url}",
                        playerName, command, finalUri);

                    using var resp = await _httpClient
                        .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, t)
                        .ConfigureAwait(false);

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(LogEvents.VlcNonSuccess,
                            "VLC non-success → Player {Player} Command {Command} -> {Code}",
                            playerName, command, (int)resp.StatusCode);
                        resp.EnsureSuccessStatusCode();
                    }
                }, maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);
            }
            finally
            {
                hostLock.Release();
            }
        }

        /// <summary>
        /// Plays the playlist entry at the given 1-based position.
        /// </summary>
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
            to.CancelAfter(TimeSpan.FromSeconds(10));

            var hostLock = GetHostLock(player.BaseUri);
            await hostLock.WaitAsync(to.Token).ConfigureAwait(false);
            try
            {
                var xdoc = await ExecuteWithRetryAsync(
                    t => GetXmlAsync(playlistUri, authToken, t),
                    maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

                var item = TryGetPlaylistItemByIndex(xdoc, position1Based);
                if (item is null)
                    throw new ArgumentOutOfRangeException(nameof(position1Based), $"Playlist does not contain position {position1Based}.");

                var (id, name) = item.Value;

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC → Player {Player}: playing playlist position {Pos} (id={Id}, name=\"{Name}\")",
                    playerName, position1Based, id, name);

                var playUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_play&id={Uri.EscapeDataString(id)}");

                await ExecuteWithRetryAsync(
                    t => SimpleGetAsync(playUri, authToken, t),
                    maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

                await Task.Delay(150, to.Token).ConfigureAwait(false);
            }
            finally
            {
                hostLock.Release();
            }
        }

        #endregion

        #region --- Helpers ---

        /// <summary>
        /// Reset: select last item → wait for length>0 (kurzes Polling) → seek to length-5 → force-pause → verify.
        /// </summary>
        private async Task ResetPlayerAsync(string playerName, Uri baseUri, string? password, CancellationToken ct)
        {
            var requestsRoot = new Uri(baseUri, Routes.Requests);
            var statusUri = new Uri(requestsRoot, Routes.Status);
            var playlistUri = new Uri(requestsRoot, Routes.Playlist);
            var authToken = CreateBasicToken(password);

            using var to = CancellationTokenSource.CreateLinkedTokenSource(ct);
            to.CancelAfter(TimeSpan.FromSeconds(15));

            var hostLock = GetHostLock(baseUri);
            await hostLock.WaitAsync(to.Token).ConfigureAwait(false);
            try
            {
                // 1) Playlist laden
                var xdoc = await ExecuteWithRetryAsync(
                    t => GetXmlAsync(playlistUri, authToken, t),
                    maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

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

                // 2) Abspielen
                var playUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_play&id={Uri.EscapeDataString(lastId)}");
                await ExecuteWithRetryAsync(t => SimpleGetAsync(playUri, authToken, t),
                    maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

                // 3) Kurzes Polling bis length>0 (max ~2s), damit seek sicher greift
                var lengthSec = await PollLengthAsync(statusUri, authToken, TimeSpan.FromSeconds(2.0), to.Token)
                    .ConfigureAwait(false);

                // 4) Seek auf letzte 5 Sekunden (wenn möglich)
                int? target = null;
                if (lengthSec is int L && L > 5)
                {
                    target = Math.Max(0, L - 5);
                    var seekUri = new Uri(requestsRoot, $"{Routes.Status}?command=seek&val={target.Value}");
                    await ExecuteWithRetryAsync(t => SimpleGetAsync(seekUri, authToken, t),
                        maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

                    await Task.Delay(200, to.Token).ConfigureAwait(false);
                }

                // 5) Force-Pause (immer)
                var pauseForceUri = new Uri(requestsRoot, $"{Routes.Status}?command=pl_forcepause");
                await ExecuteWithRetryAsync(t => SimpleGetAsync(pauseForceUri, authToken, t),
                    maxAttempts: 3, backoff: null, to.Token).ConfigureAwait(false);

                // 6) Verifizieren; falls noch nicht paused → noch einmal forcepause
                var pausedOk = await WaitUntilPausedAsync(statusUri, authToken, TimeSpan.FromSeconds(1.2), to.Token)
                    .ConfigureAwait(false);
                if (!pausedOk)
                {
                    _logger.LogDebug("VLC Reset → {Player}: pause verification failed, retrying pl_forcepause once.", playerName);
                    await ExecuteWithRetryAsync(t => SimpleGetAsync(pauseForceUri, authToken, t),
                        maxAttempts: 1, backoff: null, to.Token).ConfigureAwait(false);
                    await Task.Delay(150, to.Token).ConfigureAwait(false);
                }

                _logger.LogInformation(LogEvents.VlcCommandSent,
                    "VLC Reset → {Player}: item selected{SeekInfo}, paused.",
                    playerName,
                    (lengthSec is int LL && LL > 5) ? ", seeked near end" : string.Empty);
            }
            finally
            {
                hostLock.Release();
            }
        }

        private static string CreateBasicToken(string? password)
            => $":{password ?? string.Empty}".Base64Encode();

        private async Task<XDocument> GetXmlAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return XDocument.Load(stream);
        }

        private async Task SimpleGetAsync(Uri uri, string basicToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);

            using var resp = await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

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

        private static int? ParseLengthInSeconds(XDocument statusXml)
            => int.TryParse(statusXml.Root?.Element("length")?.Value, out var len) ? len : null;

        private static int? ParseTimeInSeconds(XDocument statusXml)
            => int.TryParse(statusXml.Root?.Element("time")?.Value, out var t) ? t : null;

        private enum VlcState { Unknown, Playing, Paused, Stopped }

        private static VlcState ParseState(XDocument statusXml)
        {
            var s = statusXml.Root?.Element("state")?.Value?.Trim().ToLowerInvariant();
            return s switch
            {
                "playing" => VlcState.Playing,
                "paused" => VlcState.Paused,
                "stop" or "stopped" => VlcState.Stopped,
                _ => VlcState.Unknown
            };
        }

        private async Task<bool> WaitUntilPausedAsync(Uri statusUri, string authToken, TimeSpan timeout, CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                var x = await GetXmlAsync(statusUri, authToken, ct).ConfigureAwait(false);
                if (ParseState(x) == VlcState.Paused) return true;
                await Task.Delay(120, ct).ConfigureAwait(false);
            }
            return false;
        }

        /// <summary>
        /// Polls status.xml until length is known (>0) or timeout expires.
        /// </summary>
        private async Task<int?> PollLengthAsync(Uri statusUri, string authToken, TimeSpan timeout, CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                var x = await GetXmlAsync(statusUri, authToken, ct).ConfigureAwait(false);
                var len = ParseLengthInSeconds(x);
                if (len is int L && L > 0) return L;
                await Task.Delay(120, ct).ConfigureAwait(false);
            }
            // letzte Chance – einmal noch lesen (hilft manchmal)
            var last = await GetXmlAsync(statusUri, authToken, ct).ConfigureAwait(false);
            return ParseLengthInSeconds(last);
        }

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

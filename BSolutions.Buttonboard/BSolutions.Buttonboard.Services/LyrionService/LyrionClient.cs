using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.LyrionService
{
    public sealed class LyrionClient : ILyrionClient
    {
        private readonly ILogger _logger;
        private readonly ISettingsProvider _settings;

        #region --- Constructor ---

        public LyrionClient(ILogger<LyrionClient> logger, ISettingsProvider settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #endregion

        #region -- ILyrionClient ---

        public Task<string> PlayUrlAsync(string playerName, string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("url must not be empty", nameof(url));

            var playerId = ResolvePlayerId(playerName);

            var cmd = $"{playerId} playlist play {Encode(url)}";
            return SendAsync(cmd, ct);
        }

        public Task<string> PauseAsync(string playerName, bool pause, CancellationToken ct = default)
        {
            var playerId = ResolvePlayerId(playerName);
            var cmd = $"{playerId} pause {(pause ? "1" : "0")}";
            return SendAsync(cmd, ct);
        }

        public Task<string> SetVolumeAsync(string playerName, int volumePercent, CancellationToken ct = default)
        {
            if (volumePercent is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(volumePercent), "0..100");

            var playerId = ResolvePlayerId(playerName);

            // ✅ "mixer volume <0..100>"
            var cmd = $"{playerId} mixer volume {volumePercent}";
            return SendAsync(cmd, ct);
        }

        #endregion

        #region --- Helpers ---

        private string ResolvePlayerId(string playerName)
        {
            var lyr = _settings.Lyrion ?? throw new InvalidOperationException("Lyrion settings missing");
            if (!lyr.Players.TryGetValue(playerName ?? "", out var id) || string.IsNullOrWhiteSpace(id))
                throw new ArgumentException($"Unknown Lyrion player '{playerName}' (Players map).");
            return id;
        }

        private static string Encode(string s)
        {
            // LMS CLI erwartet URL-encoded Tokens (space -> %20).
            // Uri.EscapeDataString macht genau das robuste Encoding.
            return Uri.EscapeDataString(s ?? string.Empty);
        }

        private async Task<string> SendAsync(string command, CancellationToken ct)
        {
            var lyr = _settings.Lyrion ?? throw new InvalidOperationException("Lyrion settings missing");

            var host = lyr.BaseUri.Host;
            var port = lyr.BaseUri.Port > 0 ? lyr.BaseUri.Port : 9090;

            using var client = new TcpClient { NoDelay = true };
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromSeconds(5)); // nur fürs Connect

            await client.ConnectAsync(host, port, connectCts.Token).ConfigureAwait(false);
            using var stream = client.GetStream();
            stream.ReadTimeout = 2000;   // soft timeouts
            stream.WriteTimeout = 2000;

            var sb = new StringBuilder(128);
            var buffer = ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                // Begrüßung ggf. lesen (keine Pflicht)
                await ReadLineAsync(stream, buffer, sb, TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                // Optional login
                if (!string.IsNullOrWhiteSpace(lyr.Username) || !string.IsNullOrWhiteSpace(lyr.Password))
                {
                    var loginCmd = $"login {Encode(lyr.Username ?? string.Empty)} {Encode(lyr.Password ?? string.Empty)}";
                    await WriteLineAsync(stream, loginCmd, ct).ConfigureAwait(false);
                    _ = await ReadLineAsync(stream, buffer, sb, TimeSpan.FromSeconds(1)).ConfigureAwait(false); // ignorierbar
                }

                _logger.LogInformation(LogEvents.ExecAudioPlay, "Lyrion CLI -> {Command}", command);
                await WriteLineAsync(stream, command, ct).ConfigureAwait(false);

                // Versuch, eine Antwort zu lesen – wenn keine kommt, trotzdem OK weiter
                var resp = await ReadLineAsync(stream, buffer, sb, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                if (string.IsNullOrEmpty(resp))
                    _logger.LogDebug("Lyrion CLI <- (no response, assuming success)");
                else
                    _logger.LogDebug("Lyrion CLI <- {Resp}", resp);

                return resp ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Lyrion CLI canceled: {Cmd}", command);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.OpenHabError, ex, "Lyrion CLI error for command: {Cmd}", command);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task WriteLineAsync(NetworkStream stream, string line, CancellationToken ct)
        {
            var data = Encoding.ASCII.GetBytes(line + "\n");
            await stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        private static async Task<string?> ReadLineAsync(
    NetworkStream stream,
    byte[] buffer,
    StringBuilder scratch,
    TimeSpan timeout)
        {
            scratch.Clear();
            using var timeoutCts = new CancellationTokenSource(timeout);

            try
            {
                while (!timeoutCts.IsCancellationRequested)
                {
                    if (!stream.DataAvailable)
                    {
                        await Task.Delay(20, timeoutCts.Token).ConfigureAwait(false);
                        continue;
                    }

                    var n = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token)
                                        .ConfigureAwait(false);
                    if (n == 0) break;

                    for (int i = 0; i < n; i++)
                    {
                        var ch = (char)buffer[i];
                        if (ch == '\n')
                            return scratch.ToString().TrimEnd('\r');

                        scratch.Append(ch);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout -> keine Antwort, ist okay
            }

            return scratch.Length > 0 ? scratch.ToString() : null;
        }

        #endregion
    }
}

using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.LyrionService;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// TCP client for the Lyrion (Logitech) Media Server CLI (default port 9090).
/// Sends line-based commands and optionally reads a single response line.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Resolve logical player names to IDs (MAC) from settings.
/// - URL-encode arguments that require it (e.g., URLs), not keywords.
/// - Handle optional login and soft timeouts (no response is not an error).
/// Thread-safety: the type is stateless per call and intended for transient use.
/// </remarks>
public sealed class LyrionClient : ILyrionClient
{
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settings;

    #region --- Constructor ---

    /// <summary>
    /// Creates a new <see cref="LyrionClient"/>.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="settings">Provides Lyrion connection and player mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="settings"/> is null.</exception>
    public LyrionClient(ILogger<LyrionClient> logger, ISettingsProvider settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    #endregion

    #region -- ILyrionClient ---

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if <paramref name="url"/> is null/whitespace or player is unknown.</exception>
    public Task<string> PlayUrlAsync(string playerName, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("url must not be empty", nameof(url));

        var playerId = ResolvePlayerId(playerName);
        // Keywords keep spaces; only URL is encoded.
        var cmd = $"{playerId} playlist play {Encode(url)}";
        return SendAsync(cmd, ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if player is unknown.</exception>
    public Task<string> PauseAsync(string playerName, bool pause, CancellationToken ct = default)
    {
        var playerId = ResolvePlayerId(playerName);
        var cmd = $"{playerId} pause {(pause ? "1" : "0")}";
        return SendAsync(cmd, ct);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="volumePercent"/> is not in 0..100.</exception>
    /// <exception cref="ArgumentException">Thrown if player is unknown.</exception>
    public Task<string> SetVolumeAsync(string playerName, int volumePercent, CancellationToken ct = default)
    {
        if (volumePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(volumePercent), "0..100");

        var playerId = ResolvePlayerId(playerName);
        var cmd = $"{playerId} mixer volume {volumePercent}";
        return SendAsync(cmd, ct);
    }

    #endregion

    #region --- Helpers ---

    /// <summary>
    /// Resolves the configured player ID (usually MAC) for a logical player name.
    /// </summary>
    /// <param name="playerName">Logical name as configured in settings.</param>
    /// <returns>The player ID (MAC) expected by the CLI.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Lyrion settings section is missing.</exception>
    /// <exception cref="ArgumentException">Thrown if the player name is unknown.</exception>
    private string ResolvePlayerId(string playerName)
    {
        var lyr = _settings.Lyrion ?? throw new InvalidOperationException("Lyrion settings missing");
        if (!lyr.Players.TryGetValue(playerName ?? "", out var id) || string.IsNullOrWhiteSpace(id))
            throw new ArgumentException($"Unknown Lyrion player '{playerName}' (Players map).");
        return id;
    }

    /// <summary>
    /// URL-encodes values for CLI arguments (e.g., media URLs). Keywords must not be encoded.
    /// </summary>
    private static string Encode(string s) => Uri.EscapeDataString(s ?? string.Empty);

    /// <summary>
    /// Opens a short-lived TCP connection, sends a single command line and attempts to read one response line.
    /// Missing responses are treated as success (many CLI commands are fire-and-forget).
    /// </summary>
    /// <param name="command">The full CLI command line to send (already assembled).</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>The raw response line if available; otherwise an empty string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if settings are incomplete.</exception>
    /// <exception cref="OperationCanceledException">Thrown on cooperative cancellation.</exception>
    /// <exception cref="SocketException">Thrown on connection errors.</exception>
    private async Task<string> SendAsync(string command, CancellationToken ct)
    {
        var lyr = _settings.Lyrion ?? throw new InvalidOperationException("Lyrion settings missing");

        var host = lyr.BaseUri.Host;
        var port = lyr.BaseUri.Port > 0 ? lyr.BaseUri.Port : 9090;

        using var client = new TcpClient { NoDelay = true };
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(TimeSpan.FromSeconds(5)); // connect timeout

        await client.ConnectAsync(host, port, connectCts.Token).ConfigureAwait(false);
        using var stream = client.GetStream();
        stream.ReadTimeout = 2000;
        stream.WriteTimeout = 2000;

        var sb = new StringBuilder(128);
        var buffer = ArrayPool<byte>.Shared.Rent(2048);
        try
        {
            // Optional greeting (not all builds send one).
            await ReadLineAsync(stream, buffer, sb, TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

            // Optional login
            if (!string.IsNullOrWhiteSpace(lyr.Username) || !string.IsNullOrWhiteSpace(lyr.Password))
            {
                var loginCmd = $"login {Encode(lyr.Username ?? string.Empty)} {Encode(lyr.Password ?? string.Empty)}";
                await WriteLineAsync(stream, loginCmd, ct).ConfigureAwait(false);
                _ = await ReadLineAsync(stream, buffer, sb, TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            _logger.LogInformation(LogEvents.ExecAudioPlay, "Lyrion CLI -> {Command}", command);
            await WriteLineAsync(stream, command, ct).ConfigureAwait(false);

            // Try to read a single response line; empty is acceptable.
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

    /// <summary>
    /// Writes a single line (ASCII) and flushes it.
    /// </summary>
    private static async Task WriteLineAsync(NetworkStream stream, string line, CancellationToken ct)
    {
        var data = Encoding.ASCII.GetBytes(line + "\n");
        await stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to read one line from the stream within the given timeout. Returns <c>null</c> on timeout.
    /// </summary>
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
            // Timeout -> no response, acceptable for fire-and-forget commands.
        }

        return scratch.Length > 0 ? scratch.ToString() : null;
    }

    #endregion
}
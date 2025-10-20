using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.LyrionService
{
    /// <summary>
    /// Defines a lightweight client interface for direct communication with
    /// the Lyrion Media Server (formerly Logitech Media Server) via its CLI interface (TCP port 9090).
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for:
    /// - Managing the TCP connection lifecycle
    /// - URL-encoding arguments where required (e.g. media URLs)
    /// - Handling optional authentication (username/password)
    /// - Providing best-effort resilience (no strict response dependency)
    /// </remarks>
    public interface ILyrionClient
    {
        /// <summary>
        /// Performs a best-effort global reset by pausing all configured players.
        /// </summary>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A task that completes once all <c>pause</c> commands have been issued.
        /// Individual player errors are logged but do not abort the operation.
        /// </returns>
        Task ResetAsync(CancellationToken ct = default);

        /// <summary>
        /// Starts playback of the specified media resource on the given player.
        /// </summary>
        /// <param name="playerName">Logical player name as defined in the configuration.</param>
        /// <param name="url">
        /// Media source to play (e.g. <c>http://server/music/track.mp3</c> or <c>spotify:track:…</c>).
        /// Implementations must URL-encode this argument.
        /// </param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A task that completes once the command has been sent to the Lyrion CLI.
        /// The returned string contains the raw CLI response (if any).
        /// </returns>
        Task<string> PlayUrlAsync(string playerName, string url, CancellationToken ct = default);

        /// <summary>
        /// Pauses or resumes playback on the specified player.
        /// </summary>
        /// <param name="playerName">Logical player name as defined in the configuration.</param>
        /// <param name="pause">
        /// <see langword="true"/> pauses playback; <see langword="false"/> resumes playback.
        /// </param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A task that completes once the command has been sent to the Lyrion CLI.
        /// The returned string contains the raw CLI response (if any).
        /// </returns>
        Task<string> PauseAsync(string playerName, bool pause, CancellationToken ct = default);

        /// <summary>
        /// Sets the playback volume for the specified player.
        /// </summary>
        /// <param name="playerName">Logical player name as defined in the configuration.</param>
        /// <param name="volumePercent">
        /// Target volume in percent (0–100).
        /// Values outside this range must throw an <see cref="System.ArgumentOutOfRangeException"/>.
        /// </param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A task that completes once the command has been sent to the Lyrion CLI.
        /// The returned string contains the raw CLI response (if any).
        /// </returns>
        Task<string> SetVolumeAsync(string playerName, int volumePercent, CancellationToken ct = default);
    }
}

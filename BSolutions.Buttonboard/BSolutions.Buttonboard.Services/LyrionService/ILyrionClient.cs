using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.LyrionService
{
    /// <summary>
    /// Defines a lightweight client interface for direct communication with
    /// the Lyrion Media Server (formerly Logitech Media Server) via its CLI interface (TCP port 9090).
    /// </summary>
    /// <remarks>
    /// Implementations are expected to handle URL-encoding of arguments,
    /// proper TCP connection lifecycle, and optional authentication (login user/pass).
    /// </remarks>
    public interface ILyrionClient
    {
        /// <summary>
        /// Starts playback of the specified media resource on the given player.
        /// </summary>
        /// <param name="playerName">Logical player name as defined in the configuration.</param>
        /// <param name="url">
        /// Media source to play (e.g. <c>http://server/music/track.mp3</c> or <c>spotify:track:…</c>).
        /// The implementation is responsible for URL-encoding this argument.
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
        /// <see langword="true"/> to pause playback; <see langword="false"/> to resume.
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
        /// Values outside this range should result in an <see cref="System.ArgumentOutOfRangeException"/>.
        /// </param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>
        /// A task that completes once the command has been sent to the Lyrion CLI.
        /// The returned string contains the raw CLI response (if any).
        /// </returns>
        Task<string> SetVolumeAsync(string playerName, int volumePercent, CancellationToken ct = default);
    }
}

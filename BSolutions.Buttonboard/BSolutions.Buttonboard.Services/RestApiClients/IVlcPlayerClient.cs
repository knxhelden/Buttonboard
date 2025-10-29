using BSolutions.Buttonboard.Services.Enumerations;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// Defines control operations for one or more VLC player instances via HTTP.
    /// </summary>
    public interface IVlcPlayerClient
    {
        /// <summary>
        /// Resets all configured VLC players to an idle state.
        /// </summary>
        /// <remarks>
        /// Selects the last playlist item, seeks near the end, and pauses playback.
        /// </remarks>
        /// <param name="ct">Cancellation token.</param>
        Task ResetAsync(CancellationToken ct = default);

        /// <summary>
        /// Sends a single VLC command (e.g., play, pause, stop) to the specified player.
        /// </summary>
        /// <param name="command">The logical command to execute.</param>
        /// <param name="playerName">Configured VLC player name (key in settings).</param>
        /// <param name="ct">Cancellation token.</param>
        Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default);

        /// <summary>
        /// Plays the playlist item at the given 1-based position on the specified player.
        /// </summary>
        /// <param name="playerName">Configured VLC player name (key in settings).</param>
        /// <param name="position1Based">1-based playlist position (1 = first item).</param>
        /// <param name="ct">Cancellation token.</param>
        Task PlayPlaylistItemAtAsync(string playerName, int position1Based, CancellationToken ct = default);
    }
}

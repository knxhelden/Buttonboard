using BSolutions.Buttonboard.Services.Enumerations;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// Defines the contract for sending control commands to one or more VLC player instances.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface provide control over VLC media players
    /// through their HTTP interface. It supports sending individual commands and
    /// resetting all configured players to a defined idle state.
    /// 
    /// <list type="bullet">
    ///   <item><description><see cref="VlcPlayerClient"/> – real HTTP-based implementation.</description></item>
    ///   <item><description><see cref="VlcPlayerClientMock"/> – in-memory simulation for tests or offline use.</description></item>
    /// </list>
    /// </remarks>
    public interface IVlcPlayerClient
    {
        /// <summary>
        /// Resets all configured VLC players (best-effort).
        /// </summary>
        /// <remarks>
        /// For each configured VLC instance, this method:
        /// <list type="number">
        ///   <item><description>Selects the last playlist item.</description></item>
        ///   <item><description>Seeks to its last 5 seconds (if known).</description></item>
        ///   <item><description>Pauses playback, leaving the player in a ready state.</description></item>
        /// </list>
        /// Intended to prepare all players for a synchronized scenario start.
        /// </remarks>
        /// <param name="ct">Cancellation token for cooperative shutdown.</param>
        Task ResetAsync(CancellationToken ct = default);

        /// <summary>
        /// Sends a high-level VLC control command (e.g. play, pause, stop) to a specific player.
        /// </summary>
        /// <param name="command">The logical VLC command to execute.</param>
        /// <param name="playerName">Configured VLC player name (key in <c>appsettings.json</c>).</param>
        /// <param name="ct">Cancellation token for cooperative shutdown.</param>
        Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default);
    }
}

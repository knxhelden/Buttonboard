using BSolutions.Buttonboard.Services.Enumerations;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// Defines the contract for sending control commands to a VLC player instance.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for translating a high-level
    /// <see cref="VlcPlayerCommand"/> into a request that a VLC instance can interpret.
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    /// <see cref="VlcPlayerClient"/>: sends actual HTTP requests to a VLC server.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    /// <see cref="VlcPlayerClientMock"/>: provides a simulated implementation for testing
    /// and offline scenarios.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface IVlcPlayerClient
    {
        /// <summary>
        /// Sends a VLC control <paramref name="command"/> to the specified <paramref name="playerName"/>.
        /// </summary>
        /// <param name="command">The high-level VLC command to execute (e.g., play, pause, stop).</param>
        /// <param name="playerName">The configured VLC player name (key in settings).</param>
        /// <param name="ct">Cancellation token.</param>
        Task SendCommandAsync(VlcPlayerCommand command, string playerName, CancellationToken ct = default);

    }
}

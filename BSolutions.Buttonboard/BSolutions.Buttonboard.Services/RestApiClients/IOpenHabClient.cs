using BSolutions.Buttonboard.Services.Enumerations;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// Contract for interacting with the openHAB REST API to control and inspect items.
    /// </summary>
    /// <remarks>
    /// Typical endpoints:
    /// <list type="bullet">
    ///   <item><description><c>POST /items/{itemname}</c> – send a command to an item.</description></item>
    ///   <item><description><c>GET /items/{itemname}/state</c> – read current state.</description></item>
    ///   <item><description><c>PUT /items/{itemname}/state</c> – update an item's state.</description></item>
    /// </list>
    /// Implementations SHOULD use <c>text/plain</c> for both request and response bodies.
    /// </remarks>
    public interface IOpenHabClient
    {
        /// <summary>
        /// Sends a command to an item using a strongly-typed <see cref="OpenHabCommand"/>.
        /// </summary>
        /// <param name="itemname">The openHAB item name.</param>
        /// <param name="command">The command to send (e.g., <c>ON</c>, <c>OFF</c>).</param>
        /// <param name="ct">Cancellation token.</param>
        Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default);

        /// <summary>
        /// Sends a command to an item using a raw request body (plain text).
        /// </summary>
        /// <param name="itemname">The openHAB item name.</param>
        /// <param name="requestBody">The raw command payload to POST as <c>text/plain</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default);

        /// <summary>
        /// Retrieves the current state of an item.
        /// </summary>
        /// <param name="itemname">The openHAB item name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The item's state as string, or <c>null</c> on handled errors.</returns>
        Task<string?> GetStateAsync(string itemname, CancellationToken ct = default);

        /// <summary>
        /// Updates (replaces) the state of an item.
        /// </summary>
        /// <param name="itemname">The openHAB item name.</param>
        /// <param name="command">The new state to PUT (e.g., <c>ON</c>, <c>OFF</c>).</param>
        /// <param name="ct">Cancellation token.</param>
        Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default);
    }
}

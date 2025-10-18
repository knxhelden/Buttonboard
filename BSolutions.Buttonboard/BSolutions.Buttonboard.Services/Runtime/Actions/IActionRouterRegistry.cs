namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Contract for resolving <see cref="IActionRouter"/> instances based on an action key.
    /// </summary>
    public interface IActionRouterRegistry
    {
        /// <summary>
        /// Attempts to resolve a router capable of handling the specified action key.
        /// </summary>
        /// <param name="actionKey">The unique string identifying the action (e.g., "audio.play" or "gpio.set").</param>
        /// <param name="router">When found, the resolved <see cref="IActionRouter"/> instance; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a matching router was found; otherwise <c>false</c>.</returns>
        bool TryResolve(string actionKey, out IActionRouter router);
    }
}

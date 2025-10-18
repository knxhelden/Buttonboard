namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Provides utility methods for working with action keys.
    /// </summary>
    /// <remarks>
    /// Action keys follow the pattern <c>&lt;domain&gt;.&lt;operation&gt;</c>,
    /// for example: <c>"audio.play"</c> or <c>"gpio.set"</c>.
    /// This helper class splits and normalizes such keys for consistent routing.
    /// </remarks>
    internal static class ActionKeyHelper
    {
        /// <summary>
        /// Splits an action key into its domain and operation parts.
        /// </summary>
        /// <param name="actionKey">The raw action key string to split (e.g., <c>"audio.play"</c>).</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><term><c>domain</c></term><description>The first segment before the dot (or the full key if no dot exists).</description></item>
        /// <item><term><c>op</c></term><description>The second segment after the dot (or an empty string if not present).</description></item>
        /// </list>
        /// Both parts are returned in lowercase and trimmed of whitespace.
        /// </returns>
        public static (string domain, string op) Split(string actionKey)
        {
            var key = actionKey?.Trim().ToLowerInvariant() ?? string.Empty;
            var idx = key.IndexOf('.');
            if (idx <= 0 || idx >= key.Length - 1)
                return (key, string.Empty);

            return (key[..idx], key[(idx + 1)..]);
        }
    }
}
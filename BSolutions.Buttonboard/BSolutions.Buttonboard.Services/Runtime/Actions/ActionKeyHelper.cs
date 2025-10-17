namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    internal static class ActionKeyHelper
    {
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

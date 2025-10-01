using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.Logging
{
    public static class LogEvents
    {
        // App/Service lifecycle
        public static readonly EventId LoaderStarted = new(1000, nameof(LoaderStarted));
        public static readonly EventId LoaderStopped = new(1001, nameof(LoaderStopped));

        // Assets
        public static readonly EventId AssetLoaded = new(2000, nameof(AssetLoaded));
        public static readonly EventId AssetRemoved = new(2001, nameof(AssetRemoved));
        public static readonly EventId AssetJsonInvalid = new(2002, nameof(AssetJsonInvalid));

        // Errors
        public static readonly EventId AssetLoadError = new(3000, nameof(AssetLoadError));
    }
}

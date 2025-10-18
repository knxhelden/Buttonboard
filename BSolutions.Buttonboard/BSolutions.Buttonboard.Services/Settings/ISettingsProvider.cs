namespace BSolutions.Buttonboard.Services.Settings
{
    /// <summary>
    /// Provides strongly typed access to all runtime configuration sections of Buttonboard.
    /// </summary>
    /// <remarks>
    /// The <see cref="ISettingsProvider"/> acts as a centralized abstraction over the application's
    /// configuration model (typically backed by <c>appsettings.json</c>).
    /// It exposes grouped option objects for each subsystem:
    /// <list type="bullet">
    /// <item><description><see cref="Application"/> – Core runtime and operation mode settings.</description></item>
    /// <item><description><see cref="OpenHAB"/> – Configuration for OpenHAB audio and automation endpoints.</description></item>
    /// <item><description><see cref="VLC"/> – Configuration for VLC media player instances.</description></item>
    /// <item><description><see cref="Mqtt"/> – Configuration for MQTT broker connections and topics.</description></item>
    /// </list>
    /// Implementations are expected to load, cache, and expose these values
    /// in a thread-safe manner for dependency-injected consumers.
    /// </remarks>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Gets the core application settings such as operation mode or logging behavior.
        /// </summary>
        ApplicationOptions Application { get; }

        /// <summary>
        /// Gets the configuration for OpenHAB integration, including audio players and items.
        /// </summary>
        OpenHabOptions OpenHAB { get; }

        /// <summary>
        /// Gets the configuration for VLC media players and their connection endpoints.
        /// </summary>
        VlcOptions VLC { get; }

        /// <summary>
        /// Gets the configuration for MQTT connectivity, topics, and credentials.
        /// </summary>
        MqttOptions Mqtt { get; }
    }
}

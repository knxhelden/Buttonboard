using Microsoft.Extensions.Options;

namespace BSolutions.Buttonboard.Services.Settings
{
    /// <summary>
    /// Default implementation of <see cref="ISettingsProvider"/> that exposes strongly typed
    /// configuration sections for all Buttonboard subsystems.
    /// </summary>
    /// <remarks>
    /// This provider binds to the composite <see cref="ButtonboardOptions"/> object supplied via
    /// <see cref="IOptions{TOptions}"/> and exposes its sub-sections as immutable properties.
    ///
    /// The settings are typically populated from <c>appsettings.json</c> and injected once
    /// at application startup through .NET's configuration system.
    /// </remarks>
    public sealed class SettingsProvider : ISettingsProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsProvider"/> class.
        /// </summary>
        /// <param name="options">
        /// The <see cref="IOptions{TOptions}"/> wrapper providing the loaded <see cref="ButtonboardOptions"/>.
        /// </param>
        public SettingsProvider(IOptions<ButtonboardOptions> options)
        {
            var o = options.Value;
            Application = o.Application;
            OpenHAB = o.OpenHAB;
            VLC = o.VLC;
            Mqtt = o.Mqtt;
        }

        /// <inheritdoc />
        public ApplicationOptions Application { get; }

        /// <inheritdoc />
        public OpenHabOptions OpenHAB { get; }

        /// <inheritdoc />
        public VlcOptions VLC { get; }

        /// <inheritdoc />
        public MqttOptions Mqtt { get; }
    }
}
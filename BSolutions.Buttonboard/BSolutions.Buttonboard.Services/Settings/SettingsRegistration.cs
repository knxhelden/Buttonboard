using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BSolutions.Buttonboard.Services.Settings
{
    /// <summary>
    /// Provides extension methods for registering Buttonboard configuration options and settings providers.
    /// </summary>
    /// <remarks>
    /// This static helper encapsulates all configuration bindings and validations required to initialize
    /// the Buttonboard runtime. It binds strongly typed option classes from <c>appsettings.json</c> using
    /// the .NET <see cref="IOptions{TOptions}"/> pattern and registers the central <see cref="ISettingsProvider"/>.
    ///
    /// Registered option groups:
    /// <list type="bullet">
    /// <item><description><see cref="ButtonboardOptions"/> – Global application, OpenHAB, VLC, and MQTT settings.</description></item>
    /// <item><description><see cref="ScenarioOptions"/> – Configuration for scenario setup and scene definitions.</description></item>
    /// </list>
    ///
    /// All options are validated at startup using <c>ValidateOnStart()</c> to ensure
    /// missing or inconsistent configuration data is detected early.
    /// </remarks>
    public static class SettingsRegistration
    {
        /// <summary>
        /// Adds and configures all Buttonboard option classes and settings providers.
        /// </summary>
        /// <param name="services">The DI service collection.</param>
        /// <param name="config">The application configuration root (<c>appsettings.json</c> source).</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
        /// <example>
        /// Typical usage in <c>Program.cs</c>:
        /// <code language="csharp">
        /// var builder = Host.CreateDefaultBuilder(args);
        /// builder.ConfigureServices((ctx, services) =>
        /// {
        ///     services.AddButtonboardOptions(ctx.Configuration);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddButtonboardOptions(
            this IServiceCollection services, IConfiguration config)
        {
            // Buttonboard options (core + subsystems)
            services.AddOptions<ButtonboardOptions>()
                .Bind(config)
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Application.ScenarioAssetsFolder),
                          "Application:ScenarioAssetsFolder must not be empty.")
                .ValidateOnStart();

            // Scenario options (setup and scenes)
            services.AddOptions<ScenarioOptions>()
                .Bind(config.GetSection("Scenario"))
                .ValidateDataAnnotations()
                .Validate(opts => opts.Scenes
                        .Select(s => s.Key)
                        .Distinct(System.StringComparer.OrdinalIgnoreCase)
                        .Count() == opts.Scenes.Count,
                          "Scenario:Scenes contains duplicate Keys.")
                .Validate(opts => !string.IsNullOrWhiteSpace(opts.Setup.Key),
                          "Scenario:Setup:Key must not be empty.")
                .ValidateOnStart();

            // Central settings provider
            services.AddSingleton<ISettingsProvider, SettingsProvider>();

            return services;
        }
    }
}
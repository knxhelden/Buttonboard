using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BSolutions.Buttonboard.Services.Settings;

public static class SettingsRegistration
{
    public static IServiceCollection AddButtonboardOptions(
        this IServiceCollection services, IConfiguration config)
    {
        // Buttonboard options (all settings)
        services.AddOptions<ButtonboardOptions>()
            .Bind(config)
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.Application.ScenarioAssetsFolder),
                      "Application:ScenarioAssetsFolder must not be empty.")
            .ValidateOnStart();

        // Szenario options (setup and scenes)
        services.AddOptions<ScenarioOptions>()
            .Bind(config.GetSection("Scenario"))
            .ValidateDataAnnotations()
            .Validate(opts => opts.Scenes.Select(s => s.Key).Distinct(System.StringComparer.OrdinalIgnoreCase).Count() == opts.Scenes.Count,
                      "Scenario:Scenes contains duplicate Keys.")
            .Validate(opts => !string.IsNullOrWhiteSpace(opts.Setup.Key),
                      "Scenario:Setup:Key must not be empty.")
            .ValidateOnStart();

        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        return services;
    }
}

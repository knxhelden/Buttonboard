using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSolutions.Buttonboard.Services.Settings;

public static class SettingsRegistration
{
    public static IServiceCollection AddButtonboardOptions(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<ButtonboardOptions>()
            .Bind(config)
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.Application.ScenarioAssetsFolder),
                      "Application:ScenarioAssetsFolder must not be empty.")
            .ValidateOnStart();

        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        return services;
    }
}

using BSolutions.Buttonboard.App.Extensions;
using BSolutions.Buttonboard.Scenario;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Runtimes;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.App
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.SetBasePath(context.HostingEnvironment.ContentRootPath);
                    configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddButtonboardOptions(context.Configuration);
                    services.AddHostedService<ConsoleHostedService>()
                    .AddSingleton<IScenarioAssetsLoader>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<ScenarioAssetsLoader>>();
                        var settings = sp.GetRequiredService<ISettingsProvider>();
                        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, settings.Application.ScenarioAssetsFolder));
                        Directory.CreateDirectory(path);
                        return new ScenarioAssetsLoader(logger, path);
                    })
                    .AddSingleton<GpioController>()
                    .AddSingleton<IButtonboardGpioController, ButtonboardGpioController>()
                    .AddByMode<IOpenHabClient, OpenHabClient, OpenHabClientMock>(sp =>
                    {
                        var app = sp.GetRequiredService<ISettingsProvider>().Application;
                        return app.OperationMode == OperationMode.Simulated;
                    })
                    .AddByMode<IMqttClient, MqttClient, MqttClientMock>(sp =>
                    {
                        var app = sp.GetRequiredService<ISettingsProvider>().Application;
                        return app.OperationMode == OperationMode.Simulated;
                    })
                    .AddByMode<IVlcPlayerClient, VlcPlayerClient, VlcPlayerClientMock>(sp =>
                    {
                        var app = sp.GetRequiredService<ISettingsProvider>().Application;
                        return app.OperationMode == OperationMode.Simulated;
                    })
                    .AddSingleton<IScenario, ScenarioRuntime>()
                    .AddSingleton<IScenarioAssetRuntime, ScenarioAssetRuntime>()
                    .AddSingleton<IActionExecutor, ActionExecutor>();
                })
                .RunConsoleAsync();
        }
    }
}

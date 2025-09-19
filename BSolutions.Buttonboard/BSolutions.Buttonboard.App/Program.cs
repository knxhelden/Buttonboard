using BSolutions.Buttonboard.Scenario;
using BSolutions.Buttonboard.Services.Gpio;
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
                    services.AddHostedService<ConsoleHostedService>()
                    .AddSingleton<ISettingsProvider, SettingsProvider>()
                    .AddSingleton<ISceneLoader>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<SceneLoader>>();
                        var path = Path.Combine(AppContext.BaseDirectory, "scenes");
                        return new SceneLoader(logger, path);
                    })
                    .AddSingleton<GpioController>()
                    .AddSingleton<IOpenHabClient, OpenHabClient>()
                    .AddSingleton<IMqttClient, MqttClient>()
                    .AddSingleton<IButtonboardGpioController, ButtonboardGpioController>()
                    .AddSingleton<IVlcPlayerClient, VlcPlayerClient>()
                    .AddSingleton<IScenario, ScenarioRuntime>()
                    .AddSingleton<ISceneRuntime, SceneRuntime>()
                    .AddSingleton<IActionExecutor, ActionExecutor>();
                })
                .RunConsoleAsync();
        }
    }
}

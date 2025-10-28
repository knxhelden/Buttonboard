using BSolutions.Buttonboard.App.Extensions;
using BSolutions.Buttonboard.Scenario;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.LyrionService;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Runtime;
using BSolutions.Buttonboard.Services.Runtime.Actions;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
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
            try
            {
                Log.Information("Bootstrapping host…");

                await Host.CreateDefaultBuilder(args)
                    .UseContentRoot(AppContext.BaseDirectory)
                    .UseSerilog((ctx, svcs, cfg) =>
                    {
                        cfg.ReadFrom.Configuration(ctx.Configuration)
                           .ReadFrom.Services(svcs);
                    })
                    .ConfigureAppConfiguration((context, configuration) =>
                    {
                        configuration.SetBasePath(context.HostingEnvironment.ContentRootPath);
                        configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddButtonboardOptions(context.Configuration);

                        services.AddHostedService<ConsoleHostedService>()
                        .AddSingleton<IScenarioAssetsLoader>(sp =>
                        {
                            var logger = sp.GetRequiredService<ILogger<ScenarioAssetsLoader>>();
                            var settings = sp.GetRequiredService<ISettingsProvider>();
                            var scenarioOptions = sp.GetRequiredService<IOptions<ScenarioOptions>>();

                            var path = Path.GetFullPath(
                                Path.Combine(AppContext.BaseDirectory, settings.Application.ScenarioAssetsFolder));
                            Directory.CreateDirectory(path);

                            return new ScenarioAssetsLoader(logger, path, scenarioOptions);
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
                        .AddSingleton<ILyrionClient, LyrionClient>()
                        .AddSingleton<IVlcPlayerClient, VlcPlayerClient>()
                        .AddSingleton<IScenarioRuntime, ScenarioRuntime>()
                        .AddSingleton<IScenarioAssetRuntime, ScenarioAssetRuntime>()
                        .AddSingleton<IActionRouter, AudioActionRouter>()
                        .AddSingleton<IActionRouter, VideoActionRouter>()
                        .AddSingleton<IActionRouter, GpioActionRouter>()
                        .AddSingleton<IActionRouter, MqttActionRouter>()
                        .AddSingleton<IActionRouterRegistry, ActionRouterRegistry>()
                        .AddSingleton<IActionExecutor, ActionExecutor>();
                    })
                    .RunConsoleAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

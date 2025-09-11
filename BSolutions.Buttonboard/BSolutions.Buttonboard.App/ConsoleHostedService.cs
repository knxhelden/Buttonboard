using BSolutions.Buttonboard.Scenario;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.App
{
    internal sealed class ConsoleHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IButtonboardMqttClient _mqtt;
        private readonly IScenario _scenario;
        private CancellationTokenSource? _cts;
        private Task? _runTask;

        public ConsoleHostedService(
            ILogger<ConsoleHostedService> logger,
            IButtonboardMqttClient mqtt,
            IScenario scenario)
        {
            _logger = logger;
            _mqtt = mqtt;
            _scenario = scenario;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting…");

            // Linked CTS to combine host shutdown token (Ctrl+C) and custom cancellation requests
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _cts.Token;

            // 1) Establish MQTT connection (ManagedClient will auto-reconnect in the background)
            await _mqtt.ConnectAsync();

            // 2) Prepare scenario
            await _scenario.ResetAsync();   // if available: await _scenario.ResetAsync(ct);
            await _scenario.SetupAsync();   // if available: await _scenario.SetupAsync(ct);

            // 3) Start long-running task in background (non-blocking)
            _runTask = Task.Run(() => _scenario.RunAsync(), ct); // if available: () => _scenario.RunAsync(ct)

            // StartAsync returns immediately → Host can respond to Ctrl+C
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping…");

            try
            {
                // Request cancellation
                _cts?.Cancel();

                // Optional: scenario cleanup
                await _scenario.ResetAsync(); // if available: await _scenario.ResetAsync(cancellationToken);

                // Wait for the run task to finish (with timeout to avoid blocking shutdown)
                if (_runTask != null)
                {
                    var completed = await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(10), cancellationToken));
                    if (completed != _runTask)
                        _logger.LogWarning("RunAsync did not stop within timeout.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during StopAsync.");
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}

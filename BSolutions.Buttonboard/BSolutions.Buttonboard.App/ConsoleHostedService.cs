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
            _logger.LogInformation("Starting runtime…");

            // Create a linked token so Ctrl+C and internal cancels are observed together
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _cts.Token;

            // Start MQTT (managed client will auto-reconnect in the background)
            await _mqtt.ConnectAsync();

            // Prepare scenario
            await _scenario.ResetAsync(ct);
            await _scenario.SetupAsync(ct);

            // Start long-running scenario without blocking host startup
            _runTask = _scenario.RunAsync(ct);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping runtime…");

            try
            {
                // Request cooperative cancellation
                _cts?.Cancel();

                if (_runTask != null)
                {
                    // Wait for a graceful stop (adjust timeout if your scenario needs longer)
                    var completed = await Task.WhenAny(
                        _runTask,
                        Task.Delay(TimeSpan.FromSeconds(15), cancellationToken));

                    if (completed != _runTask)
                        _logger.LogWarning("RunAsync did not stop within timeout.");
                }

                // Optional final cleanup
                await _scenario.ResetAsync(cancellationToken);
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
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

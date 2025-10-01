using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.Logging;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    /// <summary>
    /// HTTP client for interacting with the openHAB REST API.
    /// Base address is taken from <see cref="ISettingsProvider"/> (<c>OpenHAB.BaseUri</c>).
    /// Uses <c>text/plain</c> for commands and states.
    /// On handled failures, turns on <see cref="Led.SystemYellow"/> as a visual warning.
    /// </summary>
    public class OpenHabClient : RestApiClientBase, IOpenHabClient
    {
        private static readonly MediaTypeWithQualityHeaderValue PlainText = new("text/plain");
        private readonly IButtonboardGpioController _gpio;

        /// <summary>
        /// Creates a new <see cref="OpenHabClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="settingsProvider">Provides <c>OpenHAB.BaseUri</c> for the underlying <see cref="_httpClient"/>.</param>
        /// <param name="gpio">GPIO controller used to signal failures via LEDs.</param>
        public OpenHabClient(
            ILogger<OpenHabClient> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpio)
            : base(logger, settingsProvider) // ILogger<OpenHabClient> is covariant to ILogger<RestApiClientBase>
        {
            _httpClient.BaseAddress = _settings.OpenHAB.BaseUri;
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        }

        /// <inheritdoc />
        public Task SendCommandAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
            => SendCommandAsync(itemname, command.ToString(), ct);

        /// <inheritdoc />
        public async Task SendCommandAsync(string itemname, string requestBody, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));

            var relativeUri = $"items/{itemname}";
            using var content = new StringContent(requestBody ?? string.Empty, Encoding.UTF8, "text/plain");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, relativeUri) { Content = content };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(PlainText);

                _logger.LogInformation(LogEvents.OpenHabCommandSent,
                    "openHAB POST command Item {Item} BodyLength {Length}",
                    itemname, requestBody?.Length ?? 0);

                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(LogEvents.OpenHabNonSuccess,
                        "openHAB non-success POST Item {Item} -> {StatusCode}",
                        itemname, (int)response.StatusCode);

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    response.EnsureSuccessStatusCode(); // throws HttpRequestException
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB request canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.OpenHabError, ex,
                    "openHAB request failed POST Item {Item}", itemname);
                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetStateAsync(string itemname, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));

            var relativeUri = $"items/{itemname}/state";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(PlainText);

                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(LogEvents.OpenHabNonSuccess,
                        "openHAB non-success GET state Item {Item} -> {StatusCode}",
                        itemname, (int)response.StatusCode);

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    response.EnsureSuccessStatusCode();
                }

                var state = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogDebug(LogEvents.OpenHabStateRead,
                    "openHAB state read Item {Item} State {State}", itemname, state);
                return state;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB state request canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.OpenHabError, ex,
                    "openHAB request failed GET state Item {Item}", itemname);
                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                return null; // handled error per contract
            }
        }

        /// <inheritdoc />
        public async Task UpdateStateAsync(string itemname, OpenHabCommand command, CancellationToken ct = default)
        {
            itemname = (itemname ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(itemname))
                throw new ArgumentException("Item name must not be null or whitespace.", nameof(itemname));

            var relativeUri = $"items/{itemname}/state";
            using var content = new StringContent(command.ToString(), Encoding.UTF8, "text/plain");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, relativeUri) { Content = content };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(PlainText);

                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(LogEvents.OpenHabNonSuccess,
                        "openHAB non-success PUT state Item {Item} -> {StatusCode}",
                        itemname, (int)response.StatusCode);

                    try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                    response.EnsureSuccessStatusCode();
                }

                _logger.LogInformation(LogEvents.OpenHabStateUpdated,
                    "openHAB state updated Item {Item} -> {Command}", itemname, command);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("openHAB update canceled: {Url}", relativeUri);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.OpenHabError, ex,
                    "openHAB request failed PUT state Item {Item}", itemname);
                try { await _gpio.LedOnAsync(Led.SystemYellow).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// In-memory mock of <see cref="IButtonboardGpioController"/> for tests and offline development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mock does not access real GPIO pins. LED and button states are held in memory.
    /// It simulates a tiny latency to better reflect asynchronous behavior.
    /// </para>
    /// <para>
    /// Test helpers are provided to control button states and inspect LED states:
    /// <see cref="SetButtonPressed(Button, bool)"/> and <see cref="GetLedState(Led)"/>.
    /// </para>
    /// </remarks>
    public sealed class ButtonboardGpioControllerMock : IButtonboardGpioController, IDisposable
    {
        private readonly ILogger<ButtonboardGpioControllerMock>? _logger;
        private readonly ConcurrentDictionary<Led, bool> _ledState = new();
        private readonly ConcurrentDictionary<Button, bool> _buttonState = new();
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="ButtonboardGpioControllerMock"/>.
        /// </summary>
        /// <param name="logger">Optional logger for simulation traces.</param>
        public ButtonboardGpioControllerMock(ILogger<ButtonboardGpioControllerMock>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes internal state for all LEDs (off) and buttons (not pressed).
        /// </summary>
        public void Initialize()
        {
            ThrowIfDisposed();

            foreach (Led led in Enum.GetValues<Led>())
                _ledState[led] = false;

            foreach (Button btn in Enum.GetValues<Button>())
                _buttonState[btn] = false;

            _initialized = true;
            _logger?.LogInformation("[SIM/GPIO] Initialized mock GPIO controller.");
        }

        /// <summary>
        /// Sets all LEDs to off.
        /// </summary>
        public Task ResetAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            foreach (var key in _ledState.Keys)
                _ledState[key] = false;

            _logger?.LogDebug("[SIM/GPIO] Reset all LEDs (off).");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Turns a given LED on.
        /// </summary>
        public async Task LedOnAsync(Led led, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            await Task.Delay(5, ct).ConfigureAwait(false); // simulate tiny latency
            _ledState[led] = true;
            _logger?.LogDebug("[SIM/GPIO] LED ON: {Led}", led);
        }

        /// <summary>
        /// Turns a given LED off.
        /// </summary>
        public async Task LedOffAsync(Led led, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            await Task.Delay(5, ct).ConfigureAwait(false);
            _ledState[led] = false;
            _logger?.LogDebug("[SIM/GPIO] LED OFF: {Led}", led);
        }

        /// <summary>
        /// Returns whether a given button is pressed.
        /// </summary>
        public bool IsButtonPressed(Button button)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            return _buttonState.TryGetValue(button, out var pressed) && pressed;
        }

        /// <summary>
        /// Blinks a predefined set of process LEDs, similar to the real controller.
        /// </summary>
        public async Task LedsBlinkingAsync(int repetitions, int intervalMs = 500, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            Led[] leds =
            {
                Led.ProcessRed1, Led.ProcessRed2, Led.ProcessRed3,
                Led.ProcessYellow1, Led.ProcessYellow2, Led.ProcessYellow3,
                Led.ProcessGreen1, Led.ProcessGreen2, Led.ProcessGreen3
            };

            for (int i = 0; i < repetitions; i++)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var led in leds) _ledState[led] = true;
                _logger?.LogTrace("[SIM/GPIO] Blink cycle {Cycle}: ON", i + 1);
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);

                foreach (var led in leds) _ledState[led] = false;
                _logger?.LogTrace("[SIM/GPIO] Blink cycle {Cycle}: OFF", i + 1);
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the pressed-state of a button (test helper).
        /// </summary>
        /// <param name="button">The button to modify.</param>
        /// <param name="pressed">Whether the button is pressed.</param>
        public void SetButtonPressed(Button button, bool pressed)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            _buttonState[button] = pressed;
            _logger?.LogDebug("[SIM/GPIO] Button {Button} -> {State}", button, pressed ? "PRESSED" : "RELEASED");
        }

        /// <summary>
        /// Returns the current logical state of a given LED (test helper).
        /// </summary>
        /// <param name="led">The LED to read.</param>
        /// <returns><c>true</c> if on; otherwise <c>false</c>.</returns>
        public bool GetLedState(Led led)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            return _ledState.TryGetValue(led, out var on) && on;
        }

        /// <summary>
        /// Disposes the mock controller and clears internal state.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _ledState.Clear();
            _buttonState.Clear();
            _logger?.LogDebug("[SIM/GPIO] Mock disposed.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ButtonboardGpioControllerMock));
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("GPIO mock is not initialized. Call Initialize() first.");
        }
    }
}

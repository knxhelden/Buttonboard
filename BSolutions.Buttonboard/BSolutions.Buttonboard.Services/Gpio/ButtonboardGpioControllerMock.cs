using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Logging;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// In-memory mock of <see cref="IButtonboardGpioController"/> for tests and offline development.
    /// Simulates LED and button states without touching real GPIO. Adds tiny delays to mimic async behavior.
    /// </summary>
    public sealed class ButtonboardGpioControllerMock : IButtonboardGpioController, IDisposable
    {
        private readonly ILogger<ButtonboardGpioControllerMock>? _logger;
        private readonly ConcurrentDictionary<Led, bool> _ledState = new();
        private readonly ConcurrentDictionary<Button, bool> _buttonState = new();
        private volatile bool _initialized;
        private volatile bool _disposed;

        /// <summary>
        /// Creates a new <see cref="ButtonboardGpioControllerMock"/>.
        /// </summary>
        /// <param name="logger">Optional logger for simulated traces.</param>
        public ButtonboardGpioControllerMock(ILogger<ButtonboardGpioControllerMock>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            ThrowIfDisposed();

            foreach (Led led in Enum.GetValues<Led>())
                _ledState[led] = false;

            foreach (Button btn in Enum.GetValues<Button>())
                _buttonState[btn] = false;

            _initialized = true;
            _logger?.LogInformation(LogEvents.GpioInitialized,
                "GPIO mock initialized: {Buttons} buttons, {Leds} LEDs",
                Enum.GetValues<Button>().Length, Enum.GetValues<Led>().Length);
        }

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            foreach (var key in _ledState.Keys)
                _ledState[key] = false;

            _logger?.LogInformation(LogEvents.GpioReset, "All LEDs set to OFF (mock)");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task LedOnAsync(Led led, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            await Task.Delay(5, ct).ConfigureAwait(false); // simulate tiny latency
            _ledState[led] = true;

            _logger?.LogInformation(LogEvents.GpioLedOn, "LED set ON {Led}", led);
        }

        /// <inheritdoc />
        public async Task LedOffAsync(Led led, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            EnsureInitialized();
            ct.ThrowIfCancellationRequested();

            await Task.Delay(5, ct).ConfigureAwait(false);
            _ledState[led] = false;

            _logger?.LogInformation(LogEvents.GpioLedOff, "LED set OFF {Led}", led);
        }

        /// <inheritdoc />
        public bool IsButtonPressed(Button button)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            var pressed = _buttonState.TryGetValue(button, out var p) && p;
            _logger?.LogDebug(LogEvents.GpioButtonRead, "Button read {Button}: {Pressed}", button, pressed);
            return pressed;
        }

        /// <inheritdoc />
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

            _logger?.LogInformation(LogEvents.GpioBlinkStart,
                "Blinking LEDs (mock) Repetitions {Repetitions} IntervalMs {IntervalMs}",
                repetitions, intervalMs);

            for (int i = 0; i < repetitions; i++)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var led in leds) _ledState[led] = true;
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);

                foreach (var led in leds) _ledState[led] = false;
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
            }

            _logger?.LogInformation(LogEvents.GpioBlinkEnd,
                "Blinking completed (mock) Repetitions {Repetitions}", repetitions);
        }

        /// <summary>
        /// Test helper: sets the pressed-state of a button.
        /// </summary>
        public void SetButtonPressed(Button button, bool pressed)
        {
            ThrowIfDisposed();
            EnsureInitialized();

            _buttonState[button] = pressed;
            _logger?.LogDebug("Button state changed {Button} -> {State}", button, pressed ? "PRESSED" : "RELEASED");
        }

        /// <summary>
        /// Test helper: gets the current logical state of a given LED.
        /// </summary>
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
            _logger?.LogDebug("GPIO mock disposed");
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
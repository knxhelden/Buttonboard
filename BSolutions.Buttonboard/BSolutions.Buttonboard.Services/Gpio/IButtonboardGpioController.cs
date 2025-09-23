using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// Defines the contract for interacting with the Buttonboard GPIO hardware.
    /// </summary>
    /// <remarks>
    /// This controller abstracts access to LEDs and buttons on the physical board.
    /// Implementations must ensure proper initialization, safe disposal of pins, 
    /// and provide asynchronous methods for LED control.
    /// </remarks>
    public interface IButtonboardGpioController
    {
        /// <summary>
        /// Initializes all button and LED pins according to the board layout.
        /// </summary>
        /// <remarks>
        /// Opens GPIO pins in the correct mode (<c>Input</c> for buttons, <c>Output</c> for LEDs)
        /// and sets all LEDs to <c>Low</c> (off) by default.
        /// </remarks>
        void Initialize();

        /// <summary>
        /// Resets all LEDs to the off state.
        /// </summary>
        /// <param name="ct">Cancellation token to abort the reset operation.</param>
        Task ResetAsync(CancellationToken ct = default);

        /// <summary>
        /// Turns a given LED on (writes <c>High</c> to the corresponding GPIO pin).
        /// </summary>
        /// <param name="led">The LED to activate.</param>
        /// <param name="ct">Cancellation token to abort the operation.</param>
        Task LedOnAsync(Led led, CancellationToken ct = default);

        /// <summary>
        /// Turns a given LED off (writes <c>Low</c> to the corresponding GPIO pin).
        /// </summary>
        /// <param name="led">The LED to deactivate.</param>
        /// <param name="ct">Cancellation token to abort the operation.</param>
        Task LedOffAsync(Led led, CancellationToken ct = default);

        /// <summary>
        /// Reads the state of a button.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns><c>true</c> if the button is pressed (GPIO <c>High</c>); otherwise, <c>false</c>.</returns>
        bool IsButtonPressed(Button button);

        /// <summary>
        /// Performs a blinking animation on the process LEDs.
        /// </summary>
        /// <param name="repetitions">The number of blink cycles.</param>
        /// <param name="intervalMs">The interval in milliseconds for on/off phases (default: 500 ms).</param>
        /// <param name="ct">Cancellation token to abort the animation.</param>
        Task LedsBlinkingAsync(int repetitions, int intervalMs = 500, CancellationToken ct = default);
    }
}

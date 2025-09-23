using BSolutions.Buttonboard.Services.Attributes;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// Logical identifiers for the physical buttons on the Buttonboard.
    /// </summary>
    /// <remarks>
    /// Each member is decorated with <see cref="ButtonboardGpioAttribute"/> which maps the
    /// logical button to a specific GPIO pin number on the target device.
    /// Use the provided helpers (e.g., an extension like <c>button.GetGpio()</c>) to resolve
    /// the configured pin at runtime.
    /// </remarks>
    public enum Button
    {
        /// <summary>
        /// Top-center action button. Mapped to GPIO pin <c>13</c>.
        /// </summary>
        [ButtonboardGpio(13)]
        TopCenter,

        /// <summary>
        /// Bottom-left button. Mapped to GPIO pin <c>27</c>.
        /// </summary>
        [ButtonboardGpio(27)]
        BottomLeft,

        /// <summary>
        /// Bottom-center button. Mapped to GPIO pin <c>4</c>.
        /// </summary>
        [ButtonboardGpio(4)]
        BottomCenter,

        /// <summary>
        /// Bottom-right button. Mapped to GPIO pin <c>21</c>.
        /// </summary>
        [ButtonboardGpio(21)]
        BottomRight
    }
}

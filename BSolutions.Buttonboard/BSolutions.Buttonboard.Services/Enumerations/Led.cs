using BSolutions.Buttonboard.Services.Attributes;

namespace BSolutions.Buttonboard.Services.Gpio
{
    /// <summary>
    /// Logical identifiers for the board's LEDs.
    /// </summary>
    /// <remarks>
    /// Each member is decorated with <see cref="ButtonboardGpioAttribute"/> that maps the LED
    /// to its physical GPIO pin number. Use your helpers (e.g., <c>led.GetGpio()</c>) to resolve
    /// the pin at runtime.
    /// <para>
    /// Groups:
    /// <list type="bullet">
    ///   <item><description><b>Process*</b>: three columns (Red/Yellow/Green) used for process/progress feedback.</description></item>
    ///   <item><description><b>Button*</b>: backlight LEDs for the physical buttons.</description></item>
    ///   <item><description><b>System*</b>: reserved for global system status (e.g., warnings, ok).</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum Led
    {
        [ButtonboardGpio(23)]
        ProcessRed1,

        [ButtonboardGpio(22)]
        ProcessRed2,

        [ButtonboardGpio(12)]
        ProcessRed3,

        [ButtonboardGpio(20)]
        ProcessYellow1,

        [ButtonboardGpio(19)]
        ProcessYellow2,

        [ButtonboardGpio(24)]
        ProcessYellow3,

        [ButtonboardGpio(25)]
        ProcessGreen1,

        [ButtonboardGpio(5)]
        ProcessGreen2,

        [ButtonboardGpio(6)]
        ProcessGreen3,

        [ButtonboardGpio(16)]
        ButtonTopCenter,

        [ButtonboardGpio(9)]
        ButtonBottomLeft,

        [ButtonboardGpio(26)]
        ButtonBottomCenter,

        [ButtonboardGpio(10)]
        ButtonBottomRight,

        [ButtonboardGpio(17)]
        SystemYellow,

        [ButtonboardGpio(18)]
        SystemGreen
    }
}

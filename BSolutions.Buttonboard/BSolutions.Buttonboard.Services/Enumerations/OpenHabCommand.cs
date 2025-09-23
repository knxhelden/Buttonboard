namespace BSolutions.Buttonboard.Services.Enumerations
{
    /// <summary>
    /// Common openHAB command values used by the Buttonboard services.
    /// </summary>
    /// <remarks>
    /// These values are serialized via <see cref="object.ToString"/> and sent as plain text
    /// to the openHAB REST API (e.g., <c>POST /items/{item}</c> or <c>PUT /items/{item}/state</c>).
    /// Exact effects can depend on the bound thing/binding and the item's type.
    /// </remarks>
    public enum OpenHabCommand
    {
        /// <summary>
        /// Turns a device on / activates a function.
        /// Common for <c>Switch</c> items. Corresponds to the string <c>ON</c>.
        /// </summary>
        ON,

        /// <summary>
        /// Turns a device off / deactivates a function.
        /// Common for <c>Switch</c> items. Corresponds to the string <c>OFF</c>.
        /// </summary>
        OFF,

        /// <summary>
        /// Moves or adjusts upward/opening direction.
        /// Typically used with <c>Rollershutter</c> items to open/raise. Corresponds to <c>UP</c>.
        /// </summary>
        UP,

        /// <summary>
        /// Moves or adjusts downward/closing direction.
        /// Typically used with <c>Rollershutter</c> items to close/lower. Corresponds to <c>DOWN</c>.
        /// </summary>
        DOWN,

        /// <summary>
        /// Toggles playback between play and pause (where supported by the binding).
        /// Typically used with player/media-control items. Corresponds to <c>PLAYPAUSE</c>.
        /// </summary>
        PLAYPAUSE
    }
}

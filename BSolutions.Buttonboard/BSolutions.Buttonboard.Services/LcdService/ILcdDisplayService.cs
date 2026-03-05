namespace BSolutions.Buttonboard.Services.LcdService
{
    /// <summary>
    /// Defines a high-level API for controlling a character based HD44780 LCD display.
    /// </summary>
    public interface ILcdDisplayService
    {
        /// <summary>
        /// Initializes the display and sets it to a usable default state.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clears the full display and moves the cursor to the home position.
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets the cursor to the given position.
        /// </summary>
        void SetCursorPosition(int column, int row);

        /// <summary>
        /// Writes text at the current cursor position.
        /// </summary>
        void Write(string text);

        /// <summary>
        /// Writes one full line with optional alignment and row cleanup.
        /// </summary>
        void WriteLine(int row, string text, LcdTextAlignment alignment = LcdTextAlignment.Left, bool clearRow = true);

        /// <summary>
        /// Writes both lines of a 2-row display in one operation.
        /// </summary>
        void WriteLines(string line1, string line2, LcdTextAlignment alignment = LcdTextAlignment.Left);
    }
}

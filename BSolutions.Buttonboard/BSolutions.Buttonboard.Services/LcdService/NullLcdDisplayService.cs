namespace BSolutions.Buttonboard.Services.LcdService
{
    /// <summary>
    /// No-op display implementation used when the optional LCD is disabled.
    /// </summary>
    public sealed class NullLcdDisplayService : ILcdDisplayService
    {
        public void Initialize() { }
        public void Clear() { }
        public void SetCursorPosition(int column, int row) { }
        public void Write(string text) { }
        public void WriteLine(int row, string text, LcdTextAlignment alignment = LcdTextAlignment.Left, bool clearRow = true) { }
        public void WriteLines(string line1, string line2, LcdTextAlignment alignment = LcdTextAlignment.Left) { }
        public void SetBacklight(bool enabled) { }
    }
}

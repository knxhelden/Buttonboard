using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Device.I2c;
using System.Threading;

namespace BSolutions.Buttonboard.Services.LcdService
{
    /// <summary>
    /// HD44780 1602 LCD service for common I2C backpacks (PCF8574 compatible).
    /// </summary>
    public sealed class LcdDisplayService : ILcdDisplayService, IDisposable
    {
        private const byte RegisterSelect = 0x01;
        private const byte EnableBit = 0x04;
        private const byte BacklightBit = 0x08;

        private readonly ILogger<LcdDisplayService> _logger;
        private readonly object _sync = new();
        private readonly int _columns;
        private readonly int _rows;
        private readonly bool _defaultBacklight;
        private readonly I2cDevice _device;

        private bool _backlight;
        private bool _initialized;
        private bool _disposed;

        public LcdDisplayService(ISettingsProvider settingsProvider, ILogger<LcdDisplayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (settingsProvider == null)
                throw new ArgumentNullException(nameof(settingsProvider));

            var config = settingsProvider.Lcd;
            _columns = config.Columns;
            _rows = config.Rows;
            _defaultBacklight = config.DefaultBacklight;

            var connection = new I2cConnectionSettings(config.BusId, config.Address);
            try
            {
                _device = I2cDevice.Create(connection);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    $"Unable to open LCD I2C device '/dev/i2c-{config.BusId}' at address 0x{config.Address:X2}. " +
                    "Please verify I2C is enabled on the Raspberry Pi (raspi-config), wiring on SDA/SCL, and appsettings Lcd:BusId/Address.",
                    ex);
            }
        }

        public void Initialize()
        {
            lock (_sync)
            {
                ThrowIfDisposed();

                Thread.Sleep(50);

                Write4Bits(0x03, false);
                Thread.Sleep(5);
                Write4Bits(0x03, false);
                Thread.Sleep(1);
                Write4Bits(0x03, false);
                Write4Bits(0x02, false);

                SendCommand(0x28); // 4-bit mode, 2 lines, 5x8 dots
                SendCommand(0x08); // display off
                SendCommand(0x01); // clear
                Thread.Sleep(2);
                SendCommand(0x06); // entry mode: increment, no shift
                _backlight = _defaultBacklight;
                WriteRaw((byte)(_backlight ? BacklightBit : 0x00));
                SendCommand(0x0C); // display on, cursor off, blink off

                _initialized = true;
                _logger.LogInformation("LCD initialized on I2C bus {BusId} address 0x{Address:X2} ({Columns}x{Rows})",
                    _device.ConnectionSettings.BusId,
                    _device.ConnectionSettings.DeviceAddress,
                    _columns,
                    _rows);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                EnsureInitialized();
                SendCommand(0x01);
                Thread.Sleep(2);
            }
        }

        public void SetCursorPosition(int column, int row)
        {
            lock (_sync)
            {
                EnsureInitialized();
                ValidatePosition(column, row);

                var (mappedColumn, mappedRow) = MapPosition(column, row);
                byte[] rowOffsets = { 0x00, 0x40, 0x14, 0x54 };
                var address = (byte)(0x80 | (mappedColumn + rowOffsets[mappedRow]));
                SendCommand(address);
            }
        }

        public void Write(string text)
        {
            lock (_sync)
            {
                EnsureInitialized();

                var output = ReverseForRotation(text ?? string.Empty);

                foreach (var ch in output)
                {
                    SendData((byte)ch);
                }
            }
        }

        public void WriteLine(int row, string text, LcdTextAlignment alignment = LcdTextAlignment.Left, bool clearRow = true)
        {
            lock (_sync)
            {
                EnsureInitialized();

                if (row < 0 || row >= _rows)
                    throw new ArgumentOutOfRangeException(nameof(row), $"Row must be between 0 and {_rows - 1}.");

                var padded = BuildLine(text, alignment);
                if (!clearRow)
                    padded = padded.TrimEnd();

                SetCursorPosition(0, row);
                Write(padded);
            }
        }

        public void WriteLines(string line1, string line2, LcdTextAlignment alignment = LcdTextAlignment.Left)
        {
            lock (_sync)
            {
                EnsureInitialized();

                WriteLine(0, line1, alignment);
                if (_rows > 1)
                {
                    WriteLine(1, line2, alignment);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _device.Dispose();
            }
        }

        private string BuildLine(string text, LcdTextAlignment alignment)
        {
            var normalized = (text ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            if (normalized.Length > _columns)
                normalized = normalized[.._columns];

            return alignment switch
            {
                LcdTextAlignment.Left => normalized.PadRight(_columns),
                LcdTextAlignment.Center => normalized.PadLeft((normalized.Length + _columns) / 2).PadRight(_columns),
                LcdTextAlignment.Right => normalized.PadLeft(_columns),
                _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
            };
        }


        private (int Column, int Row) MapPosition(int column, int row)
        {
            return (column, _rows - 1 - row);
        }

        private static string ReverseForRotation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private void SendCommand(byte command)
            => SendByte(command, registerSelect: false);

        private void SendData(byte data)
            => SendByte(data, registerSelect: true);

        private void SendByte(byte value, bool registerSelect)
        {
            Write4Bits((byte)(value >> 4), registerSelect);
            Write4Bits((byte)(value & 0x0F), registerSelect);
        }

        private void Write4Bits(byte nibble, bool registerSelect)
        {
            var payload = (byte)((nibble << 4)
                | (registerSelect ? RegisterSelect : 0x00)
                | (_backlight ? BacklightBit : 0x00));

            PulseEnable(payload);
        }

        private void PulseEnable(byte payload)
        {
            WriteRaw((byte)(payload | EnableBit));
            Thread.SpinWait(80);
            WriteRaw((byte)(payload & ~EnableBit));
            Thread.SpinWait(300);
        }

        private void WriteRaw(byte value)
            => _device.WriteByte(value);

        private void ValidatePosition(int column, int row)
        {
            if (column < 0 || column >= _columns)
                throw new ArgumentOutOfRangeException(nameof(column), $"Column must be between 0 and {_columns - 1}.");

            if (row < 0 || row >= _rows)
                throw new ArgumentOutOfRangeException(nameof(row), $"Row must be between 0 and {_rows - 1}.");
        }

        private void EnsureInitialized()
        {
            ThrowIfDisposed();
            if (!_initialized)
                throw new InvalidOperationException("LCD is not initialized. Call Initialize() first.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LcdDisplayService));
        }
    }
}

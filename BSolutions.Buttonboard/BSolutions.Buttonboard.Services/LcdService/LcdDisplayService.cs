using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Device.I2c;
using System.IO;
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
        private readonly I2cConnectionSettings _connection;
        private readonly bool _failOnError;
        private I2cDevice? _device;

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
            _failOnError = config.FailOnError;
            _connection = new I2cConnectionSettings(config.BusId, config.Address);
        }

        public void Initialize()
        {
            lock (_sync)
            {
                ThrowIfDisposed();

                try
                {
                    _device = I2cDevice.Create(_connection);
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

                    _logger.LogInformation("LCD initialized on I2C bus {BusId} address 0x{Address:X2} ({Columns}x{Rows})",
                        _connection.BusId, _connection.DeviceAddress, _columns, _rows);
                }
                catch (Exception ex) when (!_failOnError && IsHardwareAccessError(ex))
                {
                    _device?.Dispose();
                    _device = null;
                    _logger.LogWarning(ex,
                        "LCD is unavailable on I2C bus {BusId} address 0x{Address:X2}; continuing without a display. " +
                        "Check the wiring/address, set Lcd:Enabled to false, or set Lcd:FailOnError to true to abort startup.",
                        _connection.BusId, _connection.DeviceAddress);
                }

                // An unavailable optional display intentionally behaves as a no-op.
                _initialized = true;
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

                byte[] rowOffsets = { 0x00, 0x40, 0x14, 0x54 };
                var address = (byte)(0x80 | (column + rowOffsets[row]));
                SendCommand(address);
            }
        }

        public void Write(string text)
        {
            lock (_sync)
            {
                EnsureInitialized();

                foreach (var ch in text ?? string.Empty)
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


        public void SetBacklight(bool enabled)
        {
            lock (_sync)
            {
                EnsureInitialized();
                _backlight = enabled;
                WriteRaw((byte)(_backlight ? BacklightBit : 0x00));
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
                _device?.Dispose();
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
        {
            if (_device == null)
                return;

            try
            {
                _device.WriteByte(value);
            }
            catch (Exception ex) when (_initialized && !_failOnError && IsHardwareAccessError(ex))
            {
                _device.Dispose();
                _device = null;
                _logger.LogWarning(ex,
                    "LCD became unavailable on I2C bus {BusId} address 0x{Address:X2}; disabling display output.",
                    _connection.BusId, _connection.DeviceAddress);
            }
        }

        private static bool IsHardwareAccessError(Exception exception)
            => exception is IOException or UnauthorizedAccessException;

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

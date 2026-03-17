using BSolutions.Buttonboard.Services.Extensions;
using BSolutions.Buttonboard.Services.LcdService;
using BSolutions.Buttonboard.Services.Loaders;
using BSolutions.Buttonboard.Services.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    /// <summary>
    /// Routes and executes LCD actions such as <c>lcd.clear</c>, <c>lcd.write</c>, <c>lcd.line</c>, <c>lcd.lines</c> and <c>lcd.backlight</c>.
    /// </summary>
    public sealed class LcdActionRouter : IActionRouter
    {
        private readonly ILogger _logger;
        private readonly ILcdDisplayService _lcd;

        public string Domain => "lcd";

        public LcdActionRouter(
            ILogger<LcdActionRouter> logger,
            ILcdDisplayService lcd)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lcd = lcd ?? throw new ArgumentNullException(nameof(lcd));
        }

        public bool CanHandle(string actionKey)
        {
            var (domain, _) = ActionKeyHelper.Split(actionKey);
            return domain == Domain;
        }

        public Task ExecuteAsync(ScenarioStepDefinition step, CancellationToken ct)
        {
            var key = step.Action?.Trim().ToLowerInvariant() ?? string.Empty;
            var (_, op) = ActionKeyHelper.Split(key);

            try
            {
                switch (op)
                {
                    case "clear":
                        _lcd.Clear();
                        _logger.LogInformation(LogEvents.ExecLcdClear, "lcd.clear");
                        break;

                    case "write":
                        HandleWrite(step);
                        break;

                    case "line":
                        HandleWriteLine(step);
                        break;

                    case "lines":
                        HandleWriteLines(step);
                        break;

                    case "backlight":
                        HandleBacklight(step);
                        break;

                    default:
                        _logger.LogWarning(LogEvents.ExecUnknownAction, "Unknown LCD action {Action}", key);
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(LogEvents.ExecActionArgInvalid, "LCD action argument error: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.ExecActionFailed, ex, "LCD action failed for {Action}", key);
                throw;
            }

            return Task.CompletedTask;
        }

        private void HandleWrite(ScenarioStepDefinition step)
        {
            var text = step.Args.GetRequiredString("text");
            var hasRow = step.Args?.ContainsKey("row") == true;
            var hasColumn = step.Args?.ContainsKey("column") == true;

            if (hasRow || hasColumn)
            {
                var row = step.Args.GetInt("row", 0);
                var column = step.Args.GetInt("column", 0);
                _lcd.SetCursorPosition(column, row);
            }

            _lcd.Write(text);
            _logger.LogInformation(LogEvents.ExecLcdWrite, "lcd.write row={Row} column={Column} text='{Text}'",
                hasRow ? step.Args.GetInt("row", 0) : -1,
                hasColumn ? step.Args.GetInt("column", 0) : -1,
                text);
        }

        private void HandleWriteLine(ScenarioStepDefinition step)
        {
            var row = step.Args.GetRequiredInt("row");
            var text = step.Args.GetRequiredString("text");
            var alignment = ParseAlignment(step.Args.GetString("align", "left"));
            var clearRow = step.Args.GetBool("clearRow", true);

            _lcd.WriteLine(row, text, alignment, clearRow);
            _logger.LogInformation(LogEvents.ExecLcdWriteLine,
                "lcd.line row={Row} align={Align} clearRow={ClearRow} text='{Text}'",
                row, alignment, clearRow, text);
        }

        private void HandleWriteLines(ScenarioStepDefinition step)
        {
            var line1 = step.Args.GetRequiredString("line1");
            var line2 = step.Args.GetString("line2", string.Empty);
            var alignment = ParseAlignment(step.Args.GetString("align", "left"));

            _lcd.WriteLines(line1, line2, alignment);
            _logger.LogInformation(LogEvents.ExecLcdWriteLines,
                "lcd.lines align={Align} line1='{Line1}' line2='{Line2}'",
                alignment, line1, line2);
        }


        private void HandleBacklight(ScenarioStepDefinition step)
        {
            var enabled = step.Args.GetRequiredBool("enabled");

            _lcd.SetBacklight(enabled);
            _logger.LogInformation(LogEvents.ExecLcdBacklight,
                "lcd.backlight enabled={Enabled}",
                enabled);
        }

        private static LcdTextAlignment ParseAlignment(string raw)
            => raw?.Trim().ToLowerInvariant() switch
            {
                "left" => LcdTextAlignment.Left,
                "center" or "centre" => LcdTextAlignment.Center,
                "right" => LcdTextAlignment.Right,
                _ => throw new ArgumentException($"Invalid align value '{raw}'. Use left, center or right.", nameof(raw))
            };
    }
}

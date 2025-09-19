using System.Threading;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public interface IButtonboardGpioController
    {
        void Initialize();
        Task ResetAsync(CancellationToken ct = default);
        Task LedOnAsync(Led led, CancellationToken ct = default);
        Task LedOffAsync(Led led, CancellationToken ct = default);
        bool IsButtonPressed(Button button);
        Task LedsBlinkingAsync(int repetitions, int intervalMs = 500, CancellationToken ct = default);
    }
}

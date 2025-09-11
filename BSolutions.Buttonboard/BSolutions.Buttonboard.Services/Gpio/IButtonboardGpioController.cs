using System.Threading.Tasks;
using BSolutions.Buttonboard.Services.Enumerations;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public interface IButtonboardGpioController
    {
        void Initialize();
        Task ResetAsync();
        Task LedOffAsync(Led led);
        Task LedOnAsync(Led led);
        bool IsButtonPressed(Button button);
        Task LedsBlinkingAsync(int repetitions, int intervall = 500);
    }
}

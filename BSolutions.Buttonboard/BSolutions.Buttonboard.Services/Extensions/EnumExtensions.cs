using BSolutions.Buttonboard.Services.Attributes;
using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using System.Reflection;

namespace BSolutions.Buttonboard.Services.Extensions
{
    public static class EnumExtensions
    {
        public static int GetGpio(this Led led)
        {
            var type = led.GetType();
            var memberInfo = type.GetMember(led.ToString());
            return memberInfo[0].GetCustomAttribute<ButtonboardGpioAttribute>().Gpio;
        }

        public static int GetGpio(this Button button)
        {
            var type = button.GetType();
            var memberInfo = type.GetMember(button.ToString());
            return memberInfo[0].GetCustomAttribute<ButtonboardGpioAttribute>().Gpio;
        }

        public static string GetCommand(this VlcPlayerCommand command)
        {
            var type = command.GetType();
            var memberInfo = type.GetMember(command.ToString());
            return memberInfo[0].GetCustomAttribute<VlcPlayerCommandAttribute>().Command;
        }
    }
}

using BSolutions.Buttonboard.Services.Attributes;
using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using System;
using System.Reflection;

namespace BSolutions.Buttonboard.Services.Extensions
{
    public static class EnumExtensions
    {
        private static TAttr GetRequiredAttribute<TAttr>(Enum value) where TAttr : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value)
                       ?? throw new ArgumentOutOfRangeException(nameof(value), value, $"Enum value '{value}' is not defined in {type.Name}.");

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException($"Field '{name}' not found on enum {type.FullName}.");

            var attr = field.GetCustomAttribute<TAttr>(inherit: false)
                       ?? throw new InvalidOperationException($"Attribute {typeof(TAttr).Name} is missing on {type.FullName}.{name}.");

            return attr;
        }

        public static int GetGpio(this Led led)
            => GetRequiredAttribute<ButtonboardGpioAttribute>(led).Gpio;

        public static int GetGpio(this Button button)
            => GetRequiredAttribute<ButtonboardGpioAttribute>(button).Gpio;

        public static string GetCommand(this VlcPlayerCommand command)
            => GetRequiredAttribute<VlcPlayerCommandAttribute>(command).Command;
    }
}
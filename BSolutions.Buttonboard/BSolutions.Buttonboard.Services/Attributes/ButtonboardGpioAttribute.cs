using System;

namespace BSolutions.Buttonboard.Services.Attributes
{
    public class ButtonboardGpioAttribute : Attribute
    {
        public int Gpio { get; set; }

        public ButtonboardGpioAttribute(int gpio)
        {
            this.Gpio = gpio;
        }
    }
}

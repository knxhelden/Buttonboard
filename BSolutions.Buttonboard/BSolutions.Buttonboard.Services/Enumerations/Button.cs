using BSolutions.Buttonboard.Services.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public enum Button
    {
        [ButtonboardGpio(13)]
        TopCenter,

        [ButtonboardGpio(27)]
        BottomLeft,

        [ButtonboardGpio(4)]
        BottomCenter,

        [ButtonboardGpio(21)]
        BottomRight
    }
}

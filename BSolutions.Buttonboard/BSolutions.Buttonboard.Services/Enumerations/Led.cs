using BSolutions.Buttonboard.Services.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Gpio
{
    public enum Led
    {
        [ButtonboardGpio(23)]
        ProcessRed1,

        [ButtonboardGpio(22)]
        ProcessRed2,

        [ButtonboardGpio(12)]
        ProcessRed3,

        [ButtonboardGpio(20)]
        ProcessYellow1,

        [ButtonboardGpio(19)]
        ProcessYellow2,

        [ButtonboardGpio(24)]
        ProcessYellow3,

        [ButtonboardGpio(25)]
        ProcessGreen1,

        [ButtonboardGpio(5)]
        ProcessGreen2,

        [ButtonboardGpio(6)]
        ProcessGreen3,

        [ButtonboardGpio(16)]
        ButtonTopCenter,

        [ButtonboardGpio(9)]
        ButtonBottomLeft,

        [ButtonboardGpio(26)]
        ButtonBottomCenter,

        [ButtonboardGpio(10)]
        ButtonBottomRight,

        [ButtonboardGpio(17)]
        SystemYellow,

        [ButtonboardGpio(18)]
        SystemGreen
    }
}

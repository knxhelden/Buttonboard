using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Services.Attributes
{
    public class VlcPlayerCommandAttribute : Attribute
    {
        public string Command { get; set; }

        public VlcPlayerCommandAttribute(string command)
        {
            this.Command = command;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario
{
    public interface IScrenes
    {
        Task RunScene1();
        Task RunScene2();
        Task RunScene3();
        Task RunScene4();
    }
}

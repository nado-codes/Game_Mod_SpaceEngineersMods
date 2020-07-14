using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nado.TimedBlocks;

namespace TimedAssembler.Tests.Utils
{
    public class TestUtils
    {
        public static TimedBlockController GenerateController()
        {
            return GenerateController(1000, 1100);
        }

        public static TimedBlockController GenerateEmptyController()
        {
            TimedBlockController controller = new TimedBlockController(0,true);

            return controller;
        }

        public static TimedBlockController GenerateController(int start1, int start2)
        {
            TimedBlockController controller = GenerateEmptyController();

            controller.AddTime(start1, start1 + 100);
            controller.AddTime(start2, start2 + 100);

            return controller;
        }
    }

    public class Emulator_MyAPIGateway
    {

    }
}

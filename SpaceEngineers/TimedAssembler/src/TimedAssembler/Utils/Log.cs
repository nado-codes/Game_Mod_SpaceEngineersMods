using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Nado.TimedBlocks;
//using Sandbox.ModAPI;
using MyAPIGateway = TimedAssembler.Tests.Utils.Emulator_MyAPIGateway;

namespace Nado.Logs
{
    public static class Log
    {
        /// <summary>
        /// Write a message to the screen.
        /// </summary>
        /// <param name="msg"></param>

        public static void Write(string msg)
        {
            MyAPIGateway.Utilities.ShowMessage("Debug", msg);
        }

        /// <summary>
        /// Write a list of items to the screen.
        /// </summary>
        /// <param name="list"></param>
        ///

        public static void WriteList(string[] list)
        {
            if (SessionManager.IsDebug())
            {
                foreach (string item in list)
                {
                    MyAPIGateway.Utilities.ShowMessage(null, " - " + item);
                }
            }
        }
    }
}

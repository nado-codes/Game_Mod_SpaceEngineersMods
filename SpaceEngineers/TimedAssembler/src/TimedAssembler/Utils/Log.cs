using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Nado.TimedBlocks;
using Sandbox.ModAPI;

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
            //Console.WriteLine(msg);
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
                    //Console.WriteLine(" - "+item);
                    MyAPIGateway.Utilities.ShowMessage(null, " - " + item);
                }
            }
        }
    }
}

using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Common;

namespace MySecuritySystem
{
    public class Class1
    {
        IMyGridTerminalSystem GridTerminalSystem;

        public void Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.

        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {

        }

        private void FadeLights()
        {
            double value = 0;
            double valueMax = 100;

            while (true)
            {
                if (value < valueMax)
                {
                    foreach (IMyLightingBlock light in GetLights())
                    {
                        light.Enabled = !light.Enabled;
                    }
                }
                else
                    value = 0;
            }
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
        }

        List<IMyLightingBlock> GetLights()
        {
            List<IMyLightingBlock> lBlocks = new List<IMyLightingBlock>();
            List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();

            

            GridTerminalSystem.GetBlocks(allBlocks); // Where allBlocks is a list type.  

            for (int i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i] is IMyLightingBlock && allBlocks[i].CustomName.Contains("BL"))
                    lBlocks.Add(allBlocks[i] as IMyLightingBlock);
            }

            return lBlocks;
        }

    }
}

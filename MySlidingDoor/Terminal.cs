using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using MySlidingDoor.Game.ModAPI;
using System.Linq;

namespace MySlidingDoor
{
    public class Terminal
    {
        protected IMyGridTerminalSystem GridTerminalSystem;

        protected ProgrammableBlock Me;

        public struct ProgrammableBlock
        {
            IMyTextSurface _surface;

            public IMyTextSurface GetSurface(int surface)
            {
                return _surface;
            }
        }
    }
}

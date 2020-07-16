

namespace MyTorpedoBuilder
{
    using Sandbox.ModAPI.Ingame;
    using System;
    using System.Collections.Generic;
    using SpaceEngineers.Game.ModAPI;
    using System.Linq;

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

using System;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Klime.BlukatExample
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "DetectionLargeBlockBeacon", "DetectionSmallBlockBeacon")]
    public class BlukatExample : MyGameLogicComponent
    {
        private IMyBeacon timer_block; //Block object

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            timer_block = Entity as IMyBeacon;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            timer_block.EnabledChanged += new Action<IMyTerminalBlock>() { Method };

        }

        public override void UpdateOnceBeforeFrame()
        {
            if (timer_block.CubeGrid.Physics != null)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME; //Only want to apply logic to "real" grids. Not projections etc.
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (!timer_block.Enabled)
            {
                timer_block.Enabled = true; //If not enabled, switch to enabled
            }
        }

        public override void Close()
        {
            timer_block = null;
        }
    }
}

using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using ObjectBuilders.SafeZone;
using VRageMath;
using ObjectBuilders.Definitions.SafeZone;

namespace SafeZoneBlockLogic
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]

    public class SafeZoneCore : MySessionComponentBase
    {
        private bool init;
        private int delay = 0;
        public static bool delayControls;
        public static IntermodSettings IntermodConfig = new IntermodSettings();

        public override void UpdateBeforeSimulation()
        {

            //Runs every tick
            if (!init) Setup();
            if (delayControls)
            {
                if (delay >= 10)
                {
                    delay = 0;
                    delayControls = false;
                    return;
                }

                delay++;
            }

        }

        private void Setup()
        {
            init = true;
            MyAPIGateway.TerminalControls.CustomControlGetter += Controls.CreateControlsNew;
            MyAPIGateway.Utilities.RegisterMessageHandler(4330, MessageHandler);
        }

        public static void MessageHandler(object data)
        {
            try
            {
                var convertedData = data as byte[];
                var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<IntermodSettings>(convertedData);
                IntermodConfig = receivedData;
            }
            catch (Exception ex)
            {

            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= Controls.CreateControlsNew;
            MyAPIGateway.Utilities.UnregisterMessageHandler(4330, MessageHandler);
        }
    }
}
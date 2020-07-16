using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.Game;
using SpaceEngineers.Game.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace SafeZoneBlockLogic
{

    public static class ControlCreation
    {

        public static bool controlsCreated = false;
        public static bool actionCreated = false;

        public static void CreateControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {

            if (block as IMySafeZoneBlock == null || controlsCreated == true)
            {
                return;
            }

            controlsCreated = true;

            var controlList = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMySafeZoneBlock>(out controlList);
            // MyVisualScriptLogicProvider.ShowNotification("Control List Ct = " + controlList.Count, 20000, "Red");

           // controlList[14].Visible = Block => Controls.HideControls(Block);
            //controlList[15].Visible = Block => Controls.HideControls(Block);
            //controlList[16].Visible = Block => Controls.HideControls(Block);
            controlList[17].Visible = Block => Controls.HideControls(Block);
            controlList[18].Visible = Block => Controls.HideControls(Block);
            controlList[19].Visible = Block => Controls.HideControls(Block);
            controlList[20].Visible = Block => Controls.HideControls(Block);
            controlList[21].Visible = Block => Controls.HideControls(Block);
            controlList[22].Visible = Block => Controls.HideControls(Block);
            controlList[23].Visible = Block => Controls.HideControls(Block);
            controlList[24].Visible = Block => Controls.HideControls(Block);
            controlList[25].Visible = Block => Controls.HideControls(Block);

            // controlList[26].Visible = Block => Controls.HideControls(Block);
            // controlList[27].Visible = Block => Controls.HideControls(Block);
            // controlList[28].Visible = Block => Controls.HideControls(Block);
            controlList[10].Enabled = Block => Controls.CheckEnabled(Block);
        }
    }
}
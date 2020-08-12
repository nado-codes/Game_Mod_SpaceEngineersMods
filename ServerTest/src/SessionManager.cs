using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace ServerTest
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SessionManager : MySessionComponentBase
    {
        private const string MOD_NAME = "Server Test";

        private NetworkBase _netObject;
        private bool _debug = false;
        private int _timer = 0;
        private double _updateFrequencyMS = 1000;

        public override void LoadData()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                _netObject = new Client();
                Log("Created a client");
            }
            else
            {
                _netObject = new Server();
                Log("Created a server");
            }

            
        }

        private void Log(string msg)
        {
            MyLog.Default.Info(MOD_NAME+": "+msg);
        }

        protected override void UnloadData()
        {
            //_netObject
        }

        public override void UpdateBeforeSimulation()
        {
            /*if(_timer % (60 * _updateFrequencyMS/1000) == 0)
                _netObject.Update();*/

            if(_timer % 60 == 0)
                MyAPIGateway.Utilities.ShowMessage("Session","Update");

            _timer++;
        }
    }
}

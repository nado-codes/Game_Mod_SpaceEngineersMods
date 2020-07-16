using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;

namespace MyTorpedoBuilder
{
    class Program : Terminal
    {
        #region Code
        private string _shipTag;
        private IMyProjector _projector;
        private IMyTimerBlock _timer;
        private List<IMyShipWelder> _welders;
        private IMyShipConnector _torpDock;
        private List<IMyThrust> _cutters;
        private IMyPistonBase _piston;

        public enum BuildState
        {
            Idle,
            Build,
            Rise,
            Cut,
            Reset
        }

        private BuildState _buildState = BuildState.Idle;

        //..create STATE SYSTEM for reloading (init,rise,cut,dock,reset)

        public Program()
        {
            // The constructor
            Exec_Setup();
        }

        public void Save()
        {
            // Called when the program needs to save its state.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                switch (argument)
                {
                    case "TODO":
                    {
                        EchoText(
                        "==TODO==\n" +
                            " - Test timing of CUT & DOCK\n" +
                            " - Launch sequence code\n\n" +
                            "02/06"

                        );
                    }
                        break;
                    case "SETUP":
                        Exec_Setup();
                        break;
                    case "RELOAD":
                        {
                            if (_buildState == BuildState.Idle)
                                Exec_Reload(BuildState.Build);
                        }
                        break;
                    case "TERMINATE":
                    {
                        _projector.Enabled = false;

                        foreach (IMyShipWelder welder in _welders)
                        {
                            welder.Enabled = false;
                        }
                        
                        Exec_Reload(BuildState.Reset);
                    }
                        break;
                    case "TIMER_FINISH":
                        Exec_TimerFinish();
                        break;
                    case "LAUNCH":
                    {

                    }
                        break;

                }
            }
            catch (Exception e)
            {
                EchoText(e.Message);
            }

        }

        private void EchoText(string msg)
        {
            Me.GetSurface(0).WriteText(msg);
        }

        private void Exec_Reload(BuildState NewState)
        {
            _buildState = NewState;

            _timer.Enabled = true;
            _timer.StopCountdown();

            switch (NewState)
            {
                case BuildState.Build:
                {
                    if (_piston.Velocity >= 0)
                    {
                        _piston.Velocity = -_piston.Velocity;
                        _piston.Enabled = true;
                    }

                    _projector.Enabled = true;

                    foreach (IMyShipWelder welder in _welders)
                    {
                        welder.Enabled = true;
                    }

                    _timer.TriggerDelay = 30;
                    _timer.StartCountdown();
                }
                    break;
                case BuildState.Rise:
                {
                    _piston.Enabled = true;

                    if (_piston.Velocity <= 0)
                        _piston.Velocity = -_piston.Velocity;

                    _timer.TriggerDelay = 125;
                    _timer.StartCountdown();
                }
                    break;
                case BuildState.Cut:
                {
                    _torpDock.Enabled = true;

                    foreach (IMyThrust cutter in _cutters)
                    {
                        cutter.Enabled = true;
                    }

                    _projector.Enabled = false;

                    foreach (IMyShipWelder welder in _welders)
                    {
                        welder.Enabled = false;
                    }

                    _timer.TriggerDelay = 10;
                    _timer.StartCountdown();
                }
                    break;
                case BuildState.Reset:
                {
                    _torpDock.Connect();

                    foreach (IMyThrust cutter in _cutters)
                    {
                        cutter.Enabled = false;
                    }

                    _timer.TriggerDelay = 8;
                    _timer.StartCountdown();
                }
                    break;
                case BuildState.Idle:
                {

                }
                    break;
                default:
                    return;
            }
        }

        private void Exec_TimerFinish()
        {
            switch (_buildState)
            {
                case BuildState.Build:
                    Exec_Reload(BuildState.Rise);
                    break;
                case BuildState.Rise:
                    Exec_Reload(BuildState.Cut);
                    break;
                case BuildState.Cut:
                    Exec_Reload(BuildState.Reset);
                    break;
                case BuildState.Reset:
                    Exec_Reload(BuildState.Idle);
                    break;
            }
        }

        private void Exec_Setup()
        {
            _projector = GetBlocksOfTypeWithName<IMyProjector>("_MTB").FirstOrDefault();
            _timer = GetBlocksOfTypeWithName<IMyTimerBlock>("_MTB").FirstOrDefault();
            _welders = GetBlocksOfTypeWithName<IMyShipWelder>("_MTB");
            _cutters = GetBlocksOfTypeWithName<IMyThrust>("_MTB");
            _torpDock = GetBlocksOfTypeWithName<IMyShipConnector>("_MTB").FirstOrDefault();
            _piston = GetBlocksOfTypeWithName<IMyPistonBase>("_MTB").FirstOrDefault();

            EchoText(
                "==SETUP== \n" +
                "\n" +
                "Projector: " + ((_projector != null) ? "Ok ✅" : "Not found") + "\n" +
                "Timer: " + ((_timer != null) ? "Ok ✅" : "Not found") + "\n" +
                "Welders: " + ((_welders != null) ? (_welders.Count > 0) ? "Ok ✅" : "Not found" : "Not found") + " (" + _cutters.Count + ")" + "\n" +
                "Torpedo Dock: " + ((_torpDock != null) ? "Ok ✅" : "Not found") + "\n" +
                "Piston: " + ((_piston != null) ? "Ok ✅" : "Not found") + "\n" +
                "Cutters: " + ((_cutters != null) ? (_cutters.Count > 0) ? "Ok ✅" : "Not found" : "Not found") + " (" + _cutters.Count + ")"
            );

            if (_projector == null || _timer == null || _welders == null || _cutters == null || _torpDock == null)
                return;

            if (_cutters.Count == 0)
                return;

            _shipTag = ParseTag(_torpDock.CubeGrid.CustomName);

            _projector.CustomName = _shipTag + "_MTB_" + "Projector";
            _timer.CustomName = _shipTag + "_MTB_" + "Timer"; //..setup timer actions automatically?
            _torpDock.CustomName = _shipTag + "_MTB_" + "TorpedoDock";
            _piston.CustomName = _shipTag + "_MTB_" + "Piston";

            //..name the welders
            for (int i = 0; i < _welders.Count; ++i)
            {
                IMyShipWelder w = _welders[i];
                w.Enabled = false;
                w.CustomName = _shipTag + "_MTB_" + "Welder_" + (i + 1);
            }

            //..make sure all the cutters are off to begin with
            for (int i = 0; i < _cutters.Count; ++i)
            {
                IMyThrust t = _cutters[i];

                t.ThrustOverridePercentage = 1;
                t.Enabled = false;
                t.CustomName = _shipTag + "_MTB_" + "Cutter_" + (i + 1);
            }
        }

        private List<T> GetBlocksOfTypeWithName<T>(string name) where T : class, IMyTerminalBlock
        {
            List<T> blocks = new List<T>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            blocks = blocks.Where(b => b.CustomName.ToUpper().Contains(name.ToUpper())).ToList();

            return blocks;
        }

        private string ParseTag(string str)
        {
            string fullTag = str.Split('[')[1];
            int tagStart = fullTag.IndexOf('[');
            int tagEnd = Math.Max(fullTag.IndexOf(']'), fullTag.Length);

            return fullTag.Substring(tagStart + 1, tagEnd - 1);
        }
        #endregion
    }
}

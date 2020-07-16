using UpdateType = Sandbox.ModAPI.Ingame.UpdateType;
using System;
using System.Collections.Generic;
using System.Linq;
using SpaceEngineers.Game.ModAPI;
using Sandbox.ModAPI;
using Entities.Blocks;

namespace MyTorpedoBuilder
{
    /*class Program : Terminal
    {
        #region Properties

        public struct Argument
        {
            public enum ArgumentType { Event, Function }
            public ArgumentType ArgType;
            public string ArgSubtype;
            public string ArgParams;

            public Argument(string argument)
            {
                string[] argParams = argument.Split('_');

                switch (argParams[0].ToUpper())
                {
                    case "EV":
                        ArgType = ArgumentType.Event;
                        break;
                    default:
                        ArgType = ArgumentType.Function;
                        break;
                }

                ArgSubtype = argParams[1].ToUpper();
                ArgParams = (argParams.Length == 3) ? argParams[2] : "";
            }
        }

        public enum State { Idle, Reload };
        public enum ReloadState { Build, Finish };
        public enum OnOff { On, Off };

        private string _shipTag;
        private IMyProjector _projector;
        private IMyTimerBlock _timer;
        private List<IMyShipWelder> _welders;
        private IMyShipConnector _torpDock;
        private List<IMyThrust> _cutters;
        private IMyPistonBase _piston;

        private State _state = State.Idle;
        private ReloadState _reloadState = ReloadState.Finish;

        #endregion

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
                Argument arg = new Argument(argument);

                switch (arg.ArgType)
                {
                    case Argument.ArgumentType.Event:
                        HandleEvent(arg.ArgSubtype, arg.ArgParams);
                        break;
                    case Argument.ArgumentType.Function:
                        {
                            switch (arg.ArgSubtype.ToUpper())
                            {
                                case "SETUP":
                                    Exec_Setup();
                                    break;
                                case "RELOAD":
                                    Exec_Reload();
                                    break;
                            }
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

        private void HandleEvent(string argSubtype, string argParams)
        {
            switch (argSubtype.ToUpper())
            {
                case "TIMERFINISH":
                    {
                        switch (_state)
                        {
                            case State.Reload:
                                Exec_Reload(argSubtype);
                                break;
                        }
                    }
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

        private void Exec_Reload(string arg = "")
        {
            switch (arg)
            {
                case "TIMERFINISH": //..timer has elapsed
                    {
                        switch (_reloadState) //..what reload state are we in?
                        {
                            case ReloadState.Build:
                                {
                                    EchoText(
                                        "==RELOAD==" + "/n" + "\n" +

                                        "Docking..."
                                       );
                                    //..projector off
                                    _projector.Enabled = false;
                                    //..welder off
                                    SetWelders(OnOff.Off);
                                    //..cutters on
                                    //SetCutters(OnOff.On);
                                    //..lock connector
                                    _torpDock.Connect();
                                    //..change state "finish"
                                    _reloadState = ReloadState.Finish;
                                    //..set timer 5 sec
                                    _timer.TriggerDelay = 1;
                                    _timer.StartCountdown();
                                }
                                break;
                            case ReloadState.Finish:
                                {
                                    EchoText(
                                        "==RELOAD==" + "/n" + "\n" +

                                        "Ready!"
                                       );
                                    //..cutters off
                                    SetCutters(OnOff.Off);
                                }
                                break;
                        }
                    }
                    break;
                default:
                    {
                        EchoText("==RELOAD==" + "/n" + "\n" +

                                 "Building...");

                        _reloadState = ReloadState.Build;
                        _state = State.Reload;
                        _projector.Enabled = true;

                        SetWelders(OnOff.On);

                        _timer.TriggerDelay = 1;
                        _timer.StartCountdown();
                    }
                    break;
            }
        }

        private void SetCutters(OnOff setting)
        {
            switch (setting)
            {
                case OnOff.On:
                    {
                        foreach (IMyThrust t in _cutters)
                        {
                            t.ThrustOverridePercentage = 1;
                            t.Enabled = true;
                        }
                        break;
                    }
                default:
                    {
                        foreach (IMyThrust t in _cutters)
                        {
                            t.Enabled = false;
                        }
                    }
                    break;
            }


        }

        private void SetWelders(OnOff setting)
        {
            switch (setting)
            {
                case OnOff.On:
                    {
                        foreach (IMyShipWelder w in _welders)
                            w.Enabled = true;
                        break;
                    }
                default:
                    {
                        foreach (IMyShipWelder w in _welders)
                            w.Enabled = false;
                    }
                    break;
            }
        }
    }*/
}

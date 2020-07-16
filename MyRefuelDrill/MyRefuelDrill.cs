using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI;
using System.Linq;

namespace MyRefuelDrill
{
    public class Program
    {
        IMyGridTerminalSystem GridTerminalSystem;

        #region Properties
        public State _currentState;

        public IMyTimerBlock _timer;
        public IMyMotorStator _rotor;
        public IMyInteriorLight _lOn;
        public IMyInteriorLight _lOff;
        public IMyTextPanel _debugPanel;
        public List<IMyShipDrill> _drills;
        #endregion


        public enum State
        {
            Inactive, WaitReady, Active
        };

        public Program()
        {
            #region Constructor Help
            /* The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
             timer block.*/
            #endregion

            _timer = GridTerminalSystem.GetBlockWithName("MRF_Timer") as IMyTimerBlock;
            _lOn = GridTerminalSystem.GetBlockWithName("MRF_lOn") as IMyInteriorLight;
            _lOff = GridTerminalSystem.GetBlockWithName("MRF_lOff") as IMyInteriorLight;
            _rotor = GridTerminalSystem.GetBlockWithName("MRF_Rotor") as IMyMotorStator;
            _debugPanel = GridTerminalSystem.GetBlockWithName("MRF_Debug") as IMyTextPanel;

            SetDrills();
            _debugPanel.WriteText(string.Join(",",_drills.Select(d => d.Name)));
        }

        void SetDrills()
        {
            List<IMyShipDrill> drillsRaw = new List<IMyShipDrill>();

            GridTerminalSystem.GetBlocksOfType(drillsRaw);

            _drills = new List<IMyShipDrill>(drillsRaw.Where(d => d.CustomName.Contains("MRF_Drill")));
            
        }

        public void Save()
        {
            #region Save Help
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
            #endregion
        }

        public void Main(string argument, UpdateType updateSource)
        {
            SetState(GetState());

            switch (argument.ToLower())
            {
                case "TOGGLE":
                    DoArgument_Toggle();
                    break;
                case "TIMER_FINISH":
                    DoArument_TimerFinish();
                    break;
            }

            _debugPanel.WriteText(_rotor.TargetVelocityRPM + "");
        }

        void DoArgument_Toggle()
        {
            switch(_currentState)
            {
                case State.Inactive:
                    SetState(State.WaitReady);
                    break;
                case State.Active:
                case State.WaitReady:
                    SetState(State.Inactive);
                    break;
            }
        }

        void DoArument_TimerFinish()
        {
            switch(_currentState)
            {
                case State.WaitReady:
                    {
                        SetState(State.Active);
                    }
                    break;
            }
        }

        void SetState(State state)
        {
            _currentState = state;

            switch(_currentState)
            {
                case State.Active:
                    {
                        _lOn.Enabled = true;
                        _lOff.Enabled = false;

                        //active state
                        foreach (IMyShipDrill drill in _drills)
                            drill.Enabled = true;
                    }
                    break;
                case State.Inactive:
                    {
                        _timer.StopCountdown();

                        _lOn.Enabled = false;
                        _lOff.Enabled = true;

                        //inactive state
                        foreach (IMyShipDrill drill in _drills)
                            drill.Enabled = false;

                        if (_rotor.TargetVelocityRPM < 0)
                            _rotor.TargetVelocityRPM = -_rotor.TargetVelocityRPM;

                    }
                    break;
                case State.WaitReady:
                    {
                        _timer.TriggerDelay = (float)(3 * (RotorAngle() / _rotor.UpperLimitDeg));
                        _timer.StartCountdown();

                        if (_rotor.TargetVelocityRPM > 0)
                            _rotor.TargetVelocityRPM = -_rotor.TargetVelocityRPM;
                    }
                    break;
            }
        }

        double RadToDeg(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        double RotorAngle()
        {
            return RadToDeg(_rotor.Angle);
        }

        State GetState()
        {
            if (RotorAngle() > 280)
                return State.Inactive;
            else
                return State.Active;
        }
    }
}

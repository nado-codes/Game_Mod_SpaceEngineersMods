using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace MySlidingDoor
{
    class Program : Terminal
    {
        #region Code
        private IMyTimerBlock _timer;
        private IMyPistonBase _pistonInOut;
        private IMyPistonBase _pistonSlide;
        private IMyLandingGear _gear;
        private bool running = false;

        public enum State
        {
            Closed,
            Open_Initial,
            Open_Final,
            Open,
            Close_Initial,
            Close_Final
        }

        private State _state = State.Closed;

        public Program()
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

            _timer = GridTerminalSystem.GetBlockWithName("MSD_Timer") as IMyTimerBlock;
            _pistonInOut = GridTerminalSystem.GetBlockWithName("MSD_pInOut") as IMyPistonBase;
            _pistonSlide = GridTerminalSystem.GetBlockWithName("MSD_pSlide") as IMyPistonBase;
            _gear = GridTerminalSystem.GetBlockWithName("MSD_Gear") as IMyLandingGear;
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
            try
            {
                switch (argument)
                {
                    case "RUN":
                        {
                            if (!running)
                            {
                                switch (_state)
                                {
                                    case State.Closed:
                                        {
                                            _pistonInOut.Reverse();
                                            _timer.TriggerDelay = 0.5f;
                                            _timer.StartCountdown();
                                            _gear.Unlock();

                                            _state = State.Open_Initial;
                                            running = true;
                                        }
                                        break;
                                    case State.Open:
                                        {
                                            _pistonSlide.Reverse();
                                            _timer.TriggerDelay = 5f;
                                            _timer.StartCountdown();
                                            _state = State.Close_Initial;
                                            running = true;
                                        }
                                        break;
                                }

                                
                            }
                        }
                        break;
                    case "TIMER":
                        {
                            if (running)
                            {
                                switch (_state)
                                {
                                    case State.Open_Initial:
                                        {
                                            _pistonSlide.Reverse();
                                            _timer.TriggerDelay = 5f;
                                            _timer.StartCountdown();
                                            _state = State.Open_Final;
                                        }
                                        break;
                                    case State.Open_Final:
                                        {
                                            _state = State.Open;
                                            running = false;
                                        }
                                        break;
                                    case State.Close_Initial:
                                        {
                                            _pistonInOut.Reverse();
                                            _timer.TriggerDelay = 0.5f;
                                            _timer.StartCountdown();

                                            _state = State.Close_Final;
                                        }
                                        break;
                                    case State.Close_Final:
                                        {
                                            _gear.Lock();
                                            _state = State.Closed;
                                            running = false;
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                EchoText("Error: " + e.Message);
            }
        }

        private void EchoText(string msg)
        {
            Me.GetSurface(0).WriteText(msg);
        }
        #endregion
    }
}

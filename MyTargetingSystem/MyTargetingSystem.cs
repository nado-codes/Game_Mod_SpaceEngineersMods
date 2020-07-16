using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI;
using System.Linq;

namespace SpaceEngineers
{
    public class Program : Terminal
    {
        private IMyGyro _mtsGyro;
        //private bool 

        private IMyInteriorLight _mtsError;
        private IMyInteriorLight _mtsGo;

        public enum OnOff { On, Off};

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
            _mtsGyro = GridTerminalSystem.GetBlockWithName("mtsGyro") as IMyGyro;
            _mtsError = GridTerminalSystem.GetBlockWithName("mtsError") as IMyInteriorLight;
            _mtsGo = GridTerminalSystem.GetBlockWithName("mtsGo") as IMyInteriorLight;
        }

        double RadToDeg(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        double DegToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
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
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            SetIndicator(_mtsError, OnOff.Off);
            
            try
            {
                switch(argument.ToLower())
                {
                    case "turn_left":
                        ProcCMD_TurnLeft();
                        break;
                    case "turn_right":
                        ProcCMD_TurnRight();
                        break;
                    case "turn_up":
                        ProcCMD_TurnUp();
                        break;
                    case "turn_down":
                        ProcCMD_TurnDown();
                        break;
                    case "reset_vertical":
                        ProcCMD_ResetVertical();
                        break;
                    case "reset_horizontal":
                        ProcCMD_ResetHorizontal();
                        break;
                }
            }
            catch
            {
                SetIndicator(_mtsError,OnOff.On);
                SetIndicator(_mtsGo, OnOff.Off);
            }

            SetIndicator(_mtsGo, OnOff.On);
        }

        private void SetIndicator(IMyInteriorLight light, OnOff setting)
        {
            switch(setting)
            {
                case OnOff.On:
                    light.Enabled = true;
                    break;
                case OnOff.Off:
                    light.Enabled = false;
                    break;
            }
        }

        private void ProcCMD_TurnLeft()
        {
            IMyAdvancedDoor door = null;

            door.Enabled = true;
        }

        private void ProcCMD_TurnRight()
        {

        }

        private void ProcCMD_TurnUp()
        {

        }

        private void ProcCMD_TurnDown()
        {

        }

        private void ProcCMD_ResetVertical()
        {

        }

        private void ProcCMD_ResetHorizontal()
        {

        }

    }
}

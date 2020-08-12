using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Nado.TimedBlocks;
//using TimedAssembler.Emulators;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

//using MyAPIGateway = Sandbox.ModAPI.MyAPIGateway;

//using TimedAssembler.Tests.Utils.Emulators;

//using MyAPIGateway = TimedAssembler.Tests.Utils.Emulator_MyAPIGateway;

namespace Nado.Logs
{
    public static class Log
    {
        /// <summary>
        /// Write a message to the screen.
        /// </summary>
        /// <param name="msg"></param>

        public const ushort MSG_LOG = 2033;

        public static void Write(string msg,bool debugOnly = false,string sender = "Debug")
        {
            /*if (SessionManager.IsDebug() && debugOnly)
                return;*/

            if(!MyAPIGateway.Multiplayer.IsServer/* || SessionManager.IsDebug()*/)
                MyAPIGateway.Utilities.ShowMessage(sender, msg);
            else
            {
                //..broadcast a message to all players
                byte[] messageData = MyAPIGateway.Utilities.SerializeToBinary(msg);
                MyAPIGateway.Multiplayer.SendMessageToOthers(MSG_LOG, messageData);

                /*List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(players);

                foreach (IMyPlayer player in players)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(MSG_LOG, messageData, player.SteamUserId);
                }*/
            }
        }

        /// <summary>
        /// Write a list of items to the screen.
        /// </summary>
        /// <param name="list"></param>
        ///

        public static void WriteList(string[] list, bool debugOnly = false)
        {
            if (SessionManager.IsDebug() && debugOnly)
                return;

            foreach (string item in list)
            {
                MyAPIGateway.Utilities.ShowMessage(null, " - " + item);
            }
        }
    }
}

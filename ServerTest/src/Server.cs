using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace ServerTest
{
    public class Server : NetworkBase
    {
        public Server()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(GetCombMessageId(MessageType.Ping), MessageHandler);
        }

        private void MessageHandler(byte[] data)
        {
            ClientPacket packet = MyAPIGateway.Utilities.SerializeFromBinary<ClientPacket>(data);

            //List<IMyPlayer> players = new List<IMyPlayer>();
            //MyAPIGateway.Multiplayer.Players.GetPlayers(players);

            //string messageString = "Hey " + players.Single(p => p.SteamUserId == packet.SteamId) + "!";
            string messageString = "Hey there! I'm here!";
            byte[] messageData = MyAPIGateway.Utilities.SerializeToBinary(messageString);

            MyAPIGateway.Multiplayer.SendMessageToOthers(GetCombMessageId(MessageType.ServerMessage), messageData);
        }

        public override void Update()
        {
            base.Update();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace ServerTest
{
    

    public class Client : NetworkBase
    {
        public Client()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(GetCombMessageId(MessageType.ServerMessage), MessageHandler);
        }

        private void PingServer()
        {
            IMyPlayer player = MyAPIGateway.Session.Player;
            ClientPacket packet = new ClientPacket() {SteamId = player.SteamUserId};
            byte[] messageData = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(GetCombMessageId(MessageType.Ping), messageData);
            MyAPIGateway.Utilities.ShowMessage("Client","Server has been pinged...");
        }

        private void MessageHandler(byte[] data)
        {
            string response = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
            MyAPIGateway.Utilities.ShowMessage("Server", response);
        }

        public override void Update()
        {
            if (_timer % 60 == 0)
                PingServer();

            if (!_introduced)
            {
                IMyPlayer player = MyAPIGateway.Session.Player;
                MyAPIGateway.Utilities.ShowMessage("Client","Hi! I'm a client! Your name is "+ player.DisplayName+"!");
                _introduced = true;
            }

            base.Update();
        }
    }
}

using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace ND.ContestedZones
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SessionManager : MySessionComponentBase
    {
        public static readonly bool Debug = false;

        public static readonly int CONTEST_RANGE = 10000; //..range of Contested Zone (in metres)

        //..SET THE MESSAGES TO USE WHEN ENTERING CONTESTED ZONES OR USING SAFE ZONES WITHIN THEM
        public static readonly string CONTEST_MESSAGE = "WARNING: ENTERING CONTESTED ZONE";
        public static readonly string SAFEZONE_MESSAGE = "Notice: SafeZones & Welders are forbidden in contested zones.";

        public static readonly int CONTEST_MESSAGE_DISPLAYTIME = 5000;
        public static readonly int SAFEZONE_MESSAGE_DISPLAYTIME = 5000;

        //..MODIFY CONTESTED ZONE PARAMETERS HERE
        public static readonly string ZONE_LOAF = "GPS:Loaf:40500:-5330:14510:";
        public static readonly string ZONE_ICE = "GPS:IceStation:54655.678946242158:-2117.3052684502391:47040.919203979174:";
        public static readonly string ZONE_ZERO = "GPS:Zero:8941.47:-6336.52:-2150.47:";

        //..SET HOW LONG YOU WANT TO WAIT BETWEEN UPDATES (in seconds)
        public static readonly int TIMER_MAX_ENTITY_UPDATE = 1;

        public static readonly ushort PACKET_ID = 20201;

        private int _timer = 0;

        //..This is necessary due to case-sensitivity of colors in UI messages
        //..Use this rather than writing "Red" as a string
        public enum Color
        {
            Red
        }

        //..Tell the client which message to display when pinged by the server
        public enum MessageType
        {
            Contest,
            Blacklist
        }

        //..Store some info about a server message, including how long it should appear for, what color it is and what message to display
        //..We'll also track when the message was last shown, so we don't spam players every time they trigger the message event
        public class ServerMessage
        {
            private int _timer = 0;
            private string _message { get; }
            private Color _color { get; }
            private int _displayTime { get; }

            public ServerMessage(string message, int displayTime, Color color)
            {
                _message = message;
                _color = color;
                _displayTime = (displayTime / 1000);
                _timer = _displayTime * 60;
            }

            public void UpdateTimer()
            {
                if (_timer < (_displayTime * 60))
                    _timer++;
            }

            public void TryShow()
            {
                if (_timer >= (_displayTime * 60))
                {
                    MyAPIGateway.Utilities.ShowNotification(_message, _displayTime * 1000, _color.ToString());

                    _timer = 0;
                }
            }
        }

        //..We'll store some server messages in a dictionary for easy access
        //..(This is unique to each client)
        public readonly Dictionary<MessageType, ServerMessage> ServerMessages = new Dictionary<MessageType, ServerMessage>();

        private List<ContestedZone> _contestedZones = new List<ContestedZone>();


        public override void LoadData()
        {
            //..create the server messages
            ServerMessages.Add(MessageType.Contest, new ServerMessage(CONTEST_MESSAGE, CONTEST_MESSAGE_DISPLAYTIME, Color.Red));
            ServerMessages.Add(MessageType.Blacklist, new ServerMessage(SAFEZONE_MESSAGE, SAFEZONE_MESSAGE_DISPLAYTIME, Color.Red));

            //..we only need to set up the contested zone for the server
            if (MyAPIGateway.Session.IsServer)
            {
                _contestedZones.Add(new ContestedZone(ZONE_ICE,CONTEST_RANGE));
                _contestedZones.Add(new ContestedZone(ZONE_LOAF, CONTEST_RANGE));
                _contestedZones.Add(new ContestedZone(ZONE_ZERO, CONTEST_RANGE));
            }

            //..we only need to handle messages for clients. the server doesn't need to know
            if (!MyAPIGateway.Session.IsServer || Debug)
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PACKET_ID, HandleServerMessage);
        }

        protected override void UnloadData()
        {
            //..we only need to handle messages for clients. the server doesn't need to know
            if (!MyAPIGateway.Session.IsServer || Debug)
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(PACKET_ID, HandleServerMessage);
        }

        public override void UpdateBeforeSimulation()
        {
            foreach (ContestedZone zone in _contestedZones)
                zone.Update(_timer);

            //..only run this on the client
            if (!MyAPIGateway.Session.IsServer || Debug)
            {
                //..allow the server messages to be shown again after some time
                //..applies to entering contested zones and activating safe zones
                ServerMessages[MessageType.Contest].UpdateTimer();
                ServerMessages[MessageType.Blacklist].UpdateTimer();
            }

            _timer++;
        }

        /// <summary>
        /// Messages sent from the server which display a message in the HUD (Clientside)
        /// </summary>
        /// <param name="data"></param>
        private void HandleServerMessage(byte[] data)
        {
            MessageType msgType = MyAPIGateway.Utilities.SerializeFromBinary<MessageType>(data);

            switch (msgType)
            {
                case MessageType.Contest:
                        ServerMessages[MessageType.Contest].TryShow();
                    break;
                case MessageType.Blacklist:
                        ServerMessages[MessageType.Blacklist].TryShow();
                    break;
            }

        }
    }
}

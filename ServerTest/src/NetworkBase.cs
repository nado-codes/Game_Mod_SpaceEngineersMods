using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;

namespace ServerTest
{
    public enum MessageType : ushort
    {
        Ping,
        ServerMessage
    };

    public class ClientPacket
    {
        public ulong SteamId;
    }

    public class NetworkBase
    {
        protected int _timer = 0;
        protected ushort _baseMsgId = 3320;
        protected bool _introduced = false;

        public virtual void Update()
        {
            _timer++;
        }

        protected ushort GetCombMessageId(MessageType type)
        {
            return Convert.ToUInt16(_baseMsgId + (ushort) type);
        }

        
        /*public static Dictionary<int,Channel> Channels { get; private set; }

        public static void Init()
        {
            Channels = new Dictionary<int, Channel>();

            CreateChannel((long)ChannelType.TypeA);
        }

        public static void CreateChannel(long id)
        {
            int channelKey = Convert.ToInt32(id);
            long msgId = Convert.ToUInt16(_baseMsgId + id);

            Channels.Add(channelKey, new Channel(msgId));
        }*/
    }

    /*public class Channel
    {
        public long MsgId { get; }

        private ushort ShortMsgId => Convert.ToUInt16(MsgId);
        public List<Action<object>> _listeners;

        public Channel(long msgId)
        {
            MsgId = msgId;

            MyAPIGateway.Utilities.RegisterMessageHandler(MsgId,MessageHandler);
        }

        public void MessageHandler(object data)
        {

        }

        public void Post(object data)
        {
            byte[] postData = MyAPIGateway.Utilities.SerializeToBinary(data);

            if (!MyAPIGateway.Multiplayer.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToServer(ShortMsgId, postData);
            else
                MyAPIGateway.Multiplayer.SendMessageToOthers(ShortMsgId, postData);
        }

        public void AddListener(Action<object> listener)
        {
            //_listeners.Add();
        }
    }*/
}

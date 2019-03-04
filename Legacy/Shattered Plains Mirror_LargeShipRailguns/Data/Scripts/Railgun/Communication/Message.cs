using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Rexxar.Communication
{
    [ProtoInclude(1, typeof(SettingsMessage))]
    [ProtoContract]
    public abstract class Message
    {
        [ProtoMember]
        public ulong SenderId;

        public Message()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }

        public abstract void HandleServer();
        public abstract void HandleClient();
    }
}

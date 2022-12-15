using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUDPDispatcher
{
    public class HLPacketDispatcher
    {
        public static dynamic INIT(HLPacket p)
        {
            dynamic pk = new ExpandoObject();
            var ms = new MemoryStream(p.packetData);
            pk.serialization = (byte)ms.ReadByte();
            pk.networkCompression = ms.readUShort();
            pk.minProtocol = ms.readUShort();
            pk.maxProtocol = ms.readUShort();
            pk.username = ms.readString();

            return pk;
        }
    }
}

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
            pk.serialization = ms.readByte();
            pk.networkCompression = ms.readUShort();
            pk.minProtocol = ms.readUShort();
            pk.maxProtocol = ms.readUShort();
            pk.username = ms.readString();

            return pk;
        }

        public static dynamic SRP_BYTES_A(HLPacket p)
        {
            dynamic pk = new ExpandoObject();
            var ms = new MemoryStream(p.packetData);
            pk.bytes = ms.readByteString();
            pk.currentLogin = ms.readByte();

            return pk;
        }

        public static dynamic SRP_BYTES_M(HLPacket p)
        {
            dynamic pk = new ExpandoObject();
            var ms = new MemoryStream(p.packetData);
            pk.bytes = ms.readByteString();

            return pk;
        }

        public static dynamic FIRST_SRP(HLPacket p)
        {
            dynamic pk = new ExpandoObject();
            var ms = new MemoryStream(p.packetData);
            pk.salt = ms.readByteString();
            pk.key = ms.readByteString();
            pk.is_empty = ms.readBool();

            return pk;
        }
    }
}

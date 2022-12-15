using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.LLProtocolHandler;
using static MTUDPDispatcher.HLProtocolHandler;

namespace MTUDPDispatcher
{
    public class HLProtocolHandler
    {
        public enum HLPacketType
        {
            TOSERVER_INIT = 0x02,
            TOCLIENT_HELLO = 0x02
        }

        public static HLPacket? DispatchData(byte[] packet)
        {
            using (var ms = new MemoryStream(packet))
            {
                byte[] buffer = new byte[2];
                ms.Read(buffer);

                var pk = new HLPacket();
                pk.packetType = BitConverter.ToUInt16(R(buffer));

                buffer = new byte[1024];
                var read = ms.Read(buffer);
                pk.packetData = new byte[read];
                Array.Copy(buffer, pk.packetData, read);

                return pk;
            }
        }
    }

    public class HLPacket
    {
        public ushort packetType;
        public HLPacketType pType
        {
            get
            {
                return (HLPacketType)packetType;
            }
            set
            {
                packetType = (ushort)value;
            }
        }

        public byte[] packetData;
        public byte channel;
    }
}

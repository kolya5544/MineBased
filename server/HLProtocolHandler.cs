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
            TOCLIENT_HELLO = 0x02,
            TOSERVER_SRP_BYTES_A = 0x51,
            TOSERVER_SRP_BYTES_M = 0x52,
            TOCLIENT_SRP_BYTES_S_B = 0x60,
            TOCLIENT_AUTH_ACCEPT = 0x03,
            TOSERVER_FIRST_SRP = 0x50,
            TOCLIENT_ACCESS_DENIED = 0x0A,
            TOSERVER_INIT2 = 0x11
        }

        public enum HLFailureReason
        {
            SERVER_ACCESSDENIED_WRONG_PASSWORD,
            SERVER_ACCESSDENIED_UNEXPECTED_DATA,
            SERVER_ACCESSDENIED_SINGLEPLAYER,
            SERVER_ACCESSDENIED_WRONG_VERSION,
            SERVER_ACCESSDENIED_WRONG_CHARS_IN_NAME,
            SERVER_ACCESSDENIED_WRONG_NAME,
            SERVER_ACCESSDENIED_TOO_MANY_USERS,
            SERVER_ACCESSDENIED_EMPTY_PASSWORD,
            SERVER_ACCESSDENIED_ALREADY_CONNECTED,
            SERVER_ACCESSDENIED_SERVER_FAIL,
            SERVER_ACCESSDENIED_CUSTOM_STRING,
            SERVER_ACCESSDENIED_SHUTDOWN,
            SERVER_ACCESSDENIED_CRASH,
            SERVER_ACCESSDENIED_MAX,
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

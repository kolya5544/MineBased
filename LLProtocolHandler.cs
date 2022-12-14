using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTUDPDispatcher
{
    public class LLProtocolHandler
    {
        public static byte[] MagicPacket = new byte[] { 0x4f, 0x45, 0x74, 0x03 };

        public static Packet? DispatchData(byte[] rawPacket)
        {
            using (var ms = new MemoryStream(rawPacket))
            {
                byte[] magic = new byte[4];
                ms.Read(magic);
                if (!cmp(magic, MagicPacket) || ms.Length <= 7)
                {
                    return null;
                }
                byte[] peerId = new byte[2];
                ms.Read(peerId);
                ushort peer = BitConverter.ToUInt16(R(peerId));
                byte channel = (byte)ms.ReadByte();

                bool reliable = false;
                ushort seqNum = 0;
                byte pType = (byte)ms.ReadByte();

                var packet = new Packet();
                packet.peerId = peer;

                if (pType == (byte)LLPacketDispatcher.LLPacketType.TYPE_RELIABLE)
                {
                    reliable = true;
                    byte[] buffer = new byte[sizeof(ushort)];
                    ms.Read(buffer);
                    seqNum = BitConverter.ToUInt16(R(buffer));
                    pType = (byte)ms.ReadByte();
                }

                packet.reliable = reliable;
                packet.reliable_seqNum = seqNum;

                if (pType == (byte)LLPacketDispatcher.LLPacketType.TYPE_ORIGINAL)
                {
                    packet.original = true;
                }
                else if (pType == (byte)LLPacketDispatcher.LLPacketType.TYPE_CONTROL)
                {
                    packet.control = true;
                    packet.control_type = (byte)ms.ReadByte();
                    if (packet.controlType == LLPacketDispatcher.LLPacket_Control_Type.CONTROLTYPE_ACK ||
                        packet.controlType == LLPacketDispatcher.LLPacket_Control_Type.CONTROLTYPE_SET_PEER_ID)
                    {
                        byte[] data = new byte[2];
                        ms.Read(data);
                        packet.control_data = BitConverter.ToUInt16(R(data));
                    }
                }
                else if (pType == (byte)LLPacketDispatcher.LLPacketType.TYPE_SPLIT)
                {
                    packet.split = true;
                    byte[] data = new byte[2];
                    ms.Read(data);
                    packet.split_seqNum = BitConverter.ToUInt16(R(data));
                    ms.Read(data);
                    packet.split_chunk_count = BitConverter.ToUInt16(R(data));
                    ms.Read(data);
                    packet.split_chunk_num = BitConverter.ToUInt16(R(data));
                }

                packet.packetType = pType;

                var bf = new byte[1024];
                var read = ms.Read(bf);
                packet.data = new byte[read];
                Array.Copy(bf, 0, packet.data, 0, read);

                /*byte[] dataRemainder = new byte[ms.Length - 4 - 2 - 1 - 1];
                ms.Read(dataRemainder);*/

                return packet;
            }
        }

        public static byte[] R(byte[] a)
        {
            return BitConverter.IsLittleEndian ? a.Reverse().ToArray() : a;
        }

        public static bool cmp(byte[] a1, byte[] a2)
        {
            if (a1 == a2)
            {
                return true;
            }
            if ((a1 != null) && (a2 != null))
            {
                if (a1.Length != a2.Length)
                {
                    return false;
                }
                for (int i = 0; i < a1.Length; i++)
                {
                    if (a1[i] != a2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}

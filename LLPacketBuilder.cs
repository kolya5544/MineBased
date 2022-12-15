using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.LLProtocolHandler;
using static MTUDPDispatcher.LLPacketDispatcher;

namespace MTUDPDispatcher
{
    public static class LLPacketBuilder
    {
        public static void SendPacket(this UdpClient client, Packet packet)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(MagicPacket); // magic
                /*if (packet.peerId != 0)
                {
                    ms.Write(R(BitConverter.GetBytes(packet.peerId))); // peer ID
                } else
                {
                    ms.Write(R(BitConverter.GetBytes((ushort)1)));
                }*/
                ms.Write(R(BitConverter.GetBytes((ushort)1))); // peer ID is always 1 server-side.
                ms.WriteByte(packet.channel); // channel

                if (packet.reliable)
                {
                    ms.WriteByte((byte)LLPacketType.TYPE_RELIABLE);
                    ms.Write(R(BitConverter.GetBytes(packet.reliable_seqNum)));
                }

                ms.WriteByte(packet.packetType);
                
                if (packet.pType == LLPacketType.TYPE_CONTROL)
                {
                    ms.WriteByte(packet.control_type);
                    ms.Write(R(BitConverter.GetBytes(packet.control_data)));
                } else if (packet.pType == LLPacketType.TYPE_SPLIT)
                {
                    ms.Write(R(BitConverter.GetBytes(packet.split_seqNum)));
                    ms.Write(R(BitConverter.GetBytes(packet.split_chunk_count)));
                    ms.Write(R(BitConverter.GetBytes(packet.split_chunk_num)));
                }

                ms.Write(packet.data);

                client.Send(ms.ToArray(), packet.origin);
            }
        }

        public static Packet BuildConnectionSuccess(Packet prototype, ushort peerId)
        {
            var response = new Packet();
            response.peerId = 0;
            response.reliable = true;
            response.reliable_seqNum = prototype.reliable_seqNum;
            response.pType = LLPacketType.TYPE_CONTROL;
            response.controlType = LLPacket_Control_Type.CONTROLTYPE_SET_PEER_ID;
            response.control_data = peerId;
            response.origin = prototype.origin;
            return response;
        }

        public static Packet BuildReliableReply(Packet prototype)
        {
            var response = new Packet();
            response.peerId = prototype.peerId;
            response.reliable = false; // a reply to reliable packet doesn't have to be reliable.
            response.pType = LLPacketType.TYPE_CONTROL;
            response.controlType = LLPacket_Control_Type.CONTROLTYPE_ACK;
            response.control_data = prototype.reliable_seqNum;
            response.origin = prototype.origin;
            return response;
        }
    }
}

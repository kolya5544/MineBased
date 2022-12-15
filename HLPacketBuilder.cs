using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MTUDPDispatcher
{
    public static class HLPacketBuilder
    {
        public static HLPacket BuildHello(dynamic ServerInitPacketPrototype, bool isRegistered)
        {
            using (var ms = new MemoryStream())
            {
                var packet = new HLPacket();
                packet.pType = HLProtocolHandler.HLPacketType.TOCLIENT_HELLO;
                ms.WriteByte(ServerInitPacketPrototype.serialization);
                ms.writeUShort((ushort)ServerInitPacketPrototype.networkCompression);
                ms.writeUShort((ushort)ServerInitPacketPrototype.maxProtocol);
                ms.writeUInt((uint)(isRegistered ? 1 << 1 : 1 << 2)); // AUTH_MECHANISM_SRP or AUTH_MECHANISM_FIRST_SRP
                ms.writeString((string)ServerInitPacketPrototype.username);
                packet.packetData = ms.ToArray();
                packet.channel = 0; // I have no idea why.
                return packet;
            }
        }

        public static void SendPacket(this UdpClient client, HLPacket packet, PlayerClient user)
        {
            using (var ms = new MemoryStream())
            {
                var p = new Packet();
                p.peerId = user.peer_id;
                p.reliable = true;
                p.reliable_seqNum = (ushort)(user.reliable_seqNum + 1);
                p.pType = LLPacketDispatcher.LLPacketType.TYPE_ORIGINAL;
                ms.writeUShort(packet.packetType);
                ms.Write(packet.packetData);
                p.data = ms.ToArray();
                p.origin = user.endpoint;
                p.channel = packet.channel;
                client.SendPacket(p);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.DataTypes;
using static MTUDPDispatcher.HLProtocolHandler;

namespace MTUDPDispatcher
{
    public static class HLPacketBuilder
    {
        public static List<PacketQueue> queue = new();

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

        public static HLPacket BuildAuth_SB(byte[] salt, byte[] b)
        {
            using (var ms = new MemoryStream())
            {
                var packet = new HLPacket();
                packet.pType = HLProtocolHandler.HLPacketType.TOCLIENT_SRP_BYTES_S_B;
                ms.writeByteString(salt);
                ms.writeByteString(b);
                packet.packetData = ms.ToArray();
                packet.channel = 0;
                return packet;
            }
        }

        public static HLPacket BuildAuthAccept(ulong mapSeed = 12345, float tickSpeed = 0.09f)
        {
            using (var ms = new MemoryStream())
            {
                var packet = new HLPacket();
                packet.pType = HLProtocolHandler.HLPacketType.TOCLIENT_AUTH_ACCEPT;
                var coords = new v3f(0, 0, 0);
                ms.Write(coords.Serialize());
                ms.writeULong(mapSeed);
                ms.writeFloat(tickSpeed);
                ms.writeUInt(1 << 1);
                packet.packetData = ms.ToArray();
                packet.channel = 0;
                return packet;
            }
        }

        public static HLPacket BuildAccessDenied(HLFailureReason reason, string customReason = "", bool reconnect = false)
        {
            using (var ms = new MemoryStream())
            {
                var packet = new HLPacket();
                packet.pType = HLProtocolHandler.HLPacketType.TOCLIENT_ACCESS_DENIED;
                ms.WriteByte((byte)reason);
                ms.writeString(customReason);
                ms.writeBool(reconnect);
                packet.packetData = ms.ToArray();
                packet.channel = 0;
                return packet;
            }
        }

        public static void SendPacket(this UdpClient client, HLPacket packet, PlayerClient user)
        {
            Console.WriteLine($">> Packet: {packet.pType}, length: {packet.packetData.Length}");
            using (var ms = new MemoryStream())
            {
                var p = new Packet();
                p.peerId = user.peer_id;
                p.reliable = true;
                p.reliable_seqNum = (ushort)(user.reliable_seqNum);
                user.reliable_seqNum += 1;
                p.pType = LLPacketDispatcher.LLPacketType.TYPE_ORIGINAL;
                ms.writeUShort(packet.packetType);
                ms.Write(packet.packetData);
                p.data = ms.ToArray();
                p.origin = user.endpoint;
                p.channel = packet.channel;
                client.SendPacket(p);
                queue.Add(new PacketQueue() { packet = p, client = client, seqNum = p.reliable_seqNum, peerId = user.peer_id });
            }
        }
    }

    public class PacketQueue
    {
        public Packet packet;
        public ushort peerId; // * these two are used for RELIABLE
        public ushort seqNum; // *
        public long sent = Program.UnixMS(); // timestamp of when the packet was sent
        public UdpClient client; // associated client

        public void Resend()
        {
            client.SendPacket(packet);
        }
    }
}

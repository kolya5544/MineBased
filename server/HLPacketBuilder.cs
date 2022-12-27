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

        public static int chunksize_max = 500;
        public static List<Packet> SplitChunkedPacket(HLPacket packet, PlayerClient user, ushort seqNum, UdpClient client)
        {
            var count = packet.packetData.Length / chunksize_max;
            count = (packet.packetData.Length % chunksize_max == 0 ? count : count + 1);
            List<Packet> p = new List<Packet>();
            var dMs = new MemoryStream(packet.packetData);
            for (int i = 0; i < count; i++)
            {
                using (var ms = new MemoryStream())
                {
                    var newP = new Packet();
                    newP.peerId = user.peer_id;
                    newP.reliable = true;
                    newP.pType = LLPacketDispatcher.LLPacketType.TYPE_SPLIT;
                    ms.writeUShort(packet.packetType);
                    var bfr = new byte[chunksize_max]; var rd = dMs.Read(bfr);
                    if (rd != chunksize_max)
                    {
                        var nbfr = new byte[rd]; Array.Copy(bfr, nbfr, rd);
                        //newP.data = nbfr;
                        ms.Write(nbfr);
                    }
                    else
                    {
                        //newP.data = bfr;
                        ms.Write(bfr);
                    }
                    newP.data = ms.ToArray();
                    newP.origin = user.endpoint;
                    newP.channel = packet.channel;

                    newP.split_seqNum = seqNum;
                    newP.split_chunk_count = (ushort)count;
                    newP.split_chunk_num = (ushort)i;
                    /*client.SendPacket(newP);
                    lock (queue)
                    {
                        queue.Add(new PacketQueue() { packet = newP, client = client, seqNum = newP.reliable_seqNum, peerId = user.peer_id });
                    }*/
                    p.Add(newP);
                }
            }
            return p;
        }

        public static void SendPacket(this UdpClient client, HLPacket packet, PlayerClient user)
        {
            byte[] packetData = packet.packetData;
            if (packetData.Length <= chunksize_max)
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
                    ms.Write(packetData);
                    p.data = ms.ToArray();
                    p.origin = user.endpoint;
                    p.channel = packet.channel;
                    client.SendPacket(p);
                    lock (queue)
                    {
                        queue.Add(new PacketQueue() { packet = p, client = client, seqNum = p.reliable_seqNum, peerId = user.peer_id });
                    }
                }
            } else
            {
                var split = SplitChunkedPacket(packet, user, user.chunked_seqNum, client);
                Console.WriteLine($">> Split Packet: {packet.pType}, length: {packet.packetData.Length} (a total of {split.Count} packets)");
                split.ForEach((z) => client.SendPacket(z));
                user.chunked_seqNum += 1;
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

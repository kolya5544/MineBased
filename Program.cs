using System;
using System.Net;
using System.Net.Sockets;

namespace MTUDPDispatcher
{
    public class Program
    {
        public static UdpClient udpServer = new UdpClient(30001);
        public static List<PlayerClient> clients = new();

        public static void Main(string[] args)
        {
            while (true)
            {
                var remoteEP = new IPEndPoint(IPAddress.Any, 30001);
                var data = udpServer.Receive(ref remoteEP); // listen on port 30001
                var dispatched = LLProtocolHandler.DispatchData(data); dispatched.origin = remoteEP;

                ushort peerId = dispatched.peerId;

                if (LLPacketDispatcher.IsConnectionPacket(dispatched))
                {
                    // if it's an empty RELIABLE(ORIGINAL()) packet with seqID = 0, then it must be connection packet
                    // we gotta reply with new assigned peer_id_new to complete the connection
                    var response = LLPacketBuilder.BuildConnectionSuccess(dispatched, (ushort)(clients.Count + 2));
                    udpServer.SendPacket(response);
                    peerId = response.control_data;

                    var u = new PlayerClient() { endpoint = remoteEP, peer_id = peerId };
                    clients.Add(u);
                }

                if (LLPacketDispatcher.IsReliableReplyRequired(dispatched))
                {
                    // client had sent a RELIABLE(*) packet. They are waiting for our reply
                    // we just ACK it atm.
                    var response = LLPacketBuilder.BuildReliableReply(dispatched);
                    udpServer.SendPacket(response);
                }

                PlayerClient user = null;
                if (peerId != 0)
                {
                    user = clients.FirstOrDefault((z) => z.peer_id == peerId);
                    if (user == null) continue;
                    //Console.WriteLine($"PACKET from user({user.peer_id}) of length {dispatched.data.Length}");

                    // from here, we begin working with High-Level Protocol.
                    // TODO: IMPLEMENT PROPER SPLIT SUPPORT (should it be in lower level or higher level? I don't know!)
                    if (dispatched.reliable)
                    {
                        user.reliable_seqNum = dispatched.reliable_seqNum;
                    }
                    var packet = HLProtocolHandler.DispatchData(dispatched.data);
                    Console.WriteLine($"Packet: {packet.pType}, length: {packet.packetData.Length}");
                    
                    if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_INIT)
                    {
                        var initPacket = HLPacketDispatcher.INIT(packet);
                        Console.WriteLine($"user({user.peer_id})'s nickname is {initPacket.username}");
                        user.username = initPacket.username;

                        // now we have to send a TOCLIENT_HELLO message to progress the handshake
                        HLPacket hp = HLPacketBuilder.BuildHello(initPacket, true);
                        udpServer.SendPacket(hp, user);
                    }
                }
                else
                {
                    Console.WriteLine($"got data of length {dispatched.data.Length} from {remoteEP} - peer ID {dispatched.peerId}");
                }
            }
        }
    }

    public class PlayerClient
    {
        public IPEndPoint endpoint;
        public ushort peer_id;
        public string username;
        public ushort reliable_seqNum;
    }
}
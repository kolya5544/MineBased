using System;
using System.Net;
using System.Net.Sockets;

namespace MTUDPDispatcher
{
    public class Program
    {
        public static UdpClient udpServer = new UdpClient(30001);
        public static List<PlayerClient> clients = new();
        public static Database db = Database.LoadDatabase("db.json");

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
                    Console.WriteLine($"Got connection packet!");
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
                        //Console.WriteLine($"Set seqNum to {dispatched.reliable_seqNum + 1}");
                        //user.reliable_seqNum = (ushort)(dispatched.reliable_seqNum + 1);
                    }
                    var packet = HLProtocolHandler.DispatchData(dispatched.data);
                    Console.WriteLine($"<< Packet: {packet.pType}, length: {packet.packetData.Length}");
                    
                    // first packet in handshake.
                    if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_INIT)
                    {
                        var initPacket = HLPacketDispatcher.INIT(packet);
                        //if (clients.Exists((z) => z.username == initPacket.username)) continue;
                        user.username = initPacket.username;
                        Console.WriteLine($"user({user.peer_id})'s nickname is {initPacket.username}");

                        // now we have to send a TOCLIENT_HELLO message to progress the handshake
                        HLPacket hp = HLPacketBuilder.BuildHello(initPacket, db.users.Exists((z) => z.username.ToLower() == user.username.ToLower()));
                        udpServer.SendPacket(hp, user);
                    }
                    if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_SRP_BYTES_A && !user.isAuthed) // auth begins
                    {
                        var authAPacket = HLPacketDispatcher.SRP_BYTES_A(packet);
                        Console.WriteLine($"got A bytes from user({user.peer_id})");
                        user.auth = SRPManager.GotBytesA(user.username, authAPacket.bytes);

                        // now we have to send the SALT and B bytes
                        var hp = HLPacketBuilder.BuildAuth_SB(user.auth.salt, user.auth.newPublic);
                        udpServer.SendPacket(hp, user);
                    }
                    if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_SRP_BYTES_M && user.auth is not null &&
                        !user.isAuthed && user.auth.phase == SRPManager.AuthPhase.GOT_BYTES_A)
                    {
                        var authMPacket = HLPacketDispatcher.SRP_BYTES_M(packet);
                        Console.WriteLine($"got M bytes from user({user.peer_id})");

                        // now we have to send our proof (M2)
                        // actually nevermind
                        // if everything is good, just let the person in.
                        user.auth.phase = SRPManager.AuthPhase.SUCCESS_AUTH;
                        var authAccept = HLPacketBuilder.BuildAuthAccept();
                        udpServer.SendPacket(authAccept, user);
                    }
                    if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_FIRST_SRP && !user.isAuthed)
                    {
                        var registerPacket = HLPacketDispatcher.FIRST_SRP(packet);

                        Console.WriteLine($"got register packet from user({user.peer_id})");
                        if (registerPacket.is_empty) continue;
                        db.users.Add(new RegisteredUser() { username = user.username, salt = registerPacket.salt, verifier = registerPacket.key });

                        user.auth = new SRPManager();
                        user.auth.phase = SRPManager.AuthPhase.SUCCESS_AUTH;
                        var authAccept = HLPacketBuilder.BuildAuthAccept();
                        udpServer.SendPacket(authAccept, user);
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
        public ushort reliable_seqNum = 65500;
        public SRPManager auth;
        public bool isAuthed
        {
            get
            {
                return auth is not null && auth.phase == SRPManager.AuthPhase.SUCCESS_AUTH;
            }
        }
    }
}
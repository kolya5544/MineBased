using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.Program;

namespace MTUDPDispatcher
{
    public class ClientHandler
    {
        public static void Process(Packet dispatched, IPEndPoint remoteEP)
        {
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
                lock (clients)
                {
                    clients.Add(u);
                }
            }

            if (LLPacketDispatcher.IsReliableReplyRequired(dispatched))
            {
                // client had sent a RELIABLE(*) packet. They are waiting for our reply
                // we just ACK it atm.
                var response = LLPacketBuilder.BuildReliableReply(dispatched);
                udpServer.SendPacket(response);
            }

            if (LLPacketDispatcher.IsReplyToAReliable(dispatched))
            {
                // we got a reply - deleting a packet from the queue.
                HLPacketBuilder.queue.RemoveAll((z) => z.peerId == dispatched.peerId && z.seqNum == dispatched.control_data);
                // checking if there are any packets we lost
                HLPacketBuilder.queue.ForEach((z) => { if (z.sent + 2000 < UnixMS()) { z.Resend(); } });
            }

            if (dispatched.controlType == LLPacketDispatcher.LLPacket_Control_Type.CONTROLTYPE_DISCO &&
                dispatched.pType == LLPacketDispatcher.LLPacketType.TYPE_CONTROL)
            {
                // client wants to disconnect - let's figure out whom to disconnect
                lock (clients)
                {
                    var du = clients.FirstOrDefault((z) => z.peer_id == peerId);
                    if (du == null) return;
                    Disconnect(du);
                }
            }

            PlayerClient user = null;
            if (peerId != 0)
            {
                lock (clients)
                {
                    user = clients.FirstOrDefault((z) => z.peer_id == peerId);
                    if (user == null) return;
                }
                user.lastPacket = UnixMS();
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
                    lock (clients)
                    {
                        if (clients.Exists((z) => z.username is not null && z.username.ToLower() == initPacket.username.ToLower() && z.isAuthed))
                        {
                            var pk = HLPacketBuilder.BuildAccessDenied(HLProtocolHandler.HLFailureReason.SERVER_ACCESSDENIED_ALREADY_CONNECTED);
                            Disconnect(user);
                            udpServer.SendPacket(pk, user); return;
                        }
                    }
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
                    lock (clients)
                    {
                        clients.RemoveAll((z) => z.username is not null && z.username == user.username && !z.isAuthed);
                        clients.RemoveAll((z) => z is not null && !z.isAuthed && z.creation + 6000 < UnixMS()); // 6 seconds to establish the connection is plety o'time
                    }
                }
                if (packet.pType == HLProtocolHandler.HLPacketType.TOSERVER_FIRST_SRP && !user.isAuthed) // register process
                {
                    var registerPacket = HLPacketDispatcher.FIRST_SRP(packet);

                    Console.WriteLine($"got register packet from user({user.peer_id})");
                    if (registerPacket.is_empty)
                    {
                        var pk = HLPacketBuilder.BuildAccessDenied(HLProtocolHandler.HLFailureReason.SERVER_ACCESSDENIED_EMPTY_PASSWORD);
                        Disconnect(user);
                        udpServer.SendPacket(pk, user); return;
                    }
                    if (db.users.Exists((z) => z.username == user.username))
                    {

                    }
                    db.users.Add(new RegisteredUser() { username = user.username, salt = registerPacket.salt, verifier = registerPacket.key });

                    user.auth = new SRPManager();
                    user.auth.phase = SRPManager.AuthPhase.SUCCESS_AUTH;
                    var authAccept = HLPacketBuilder.BuildAuthAccept();
                    udpServer.SendPacket(authAccept, user);
                }

                // -*- THESE PACKETS WILL ONLY WORK IF THE CLIENT IS AUTHED! (marking the end of auth handshake) -*-

                if (!user.isAuthed) return;

                
            }
            else
            {
                Console.WriteLine($"got data of length {dispatched.data.Length} from {remoteEP} - peer ID {dispatched.peerId}");
            }
        }

        public static void KickAFK()
        {
            // kicks anyone disconnected or AFK
            lock (clients)
            {
                //clients.ForEach((z) => { if (z.lastPacket + 15000 > UnixMS()) Disconnect(z); });
                for (int i = 0; i < clients.Count; i++)
                {
                    var z = clients[i];
                    if (z.lastPacket + 15000 < UnixMS()) { Disconnect(z); i--; }
                }
            }
        }

        private static void Disconnect(PlayerClient user)
        {
            Console.WriteLine($"Disconnected {user.username} user({user.peer_id})");
            lock (clients)
            {
                clients.Remove(user);
            }
        }
    }
}

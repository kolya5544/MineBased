using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MTUDPDispatcher
{
    public class Program
    {
        public static UdpClient udpServer = new UdpClient(30001);
        public static List<PlayerClient> clients = new();
        public static Database db = Database.LoadDatabase("db.json");

        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                udpServer.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }

            new Thread(() =>
            {
                while (true) { Thread.Sleep(2000); ClientHandler.KickAFK(); }
            }).Start();
            while (true)
            {
                try
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 30001);
                    var data = udpServer.Receive(ref remoteEP); // listen on port 30001
                    var dispatched = LLProtocolHandler.DispatchData(data); dispatched.origin = remoteEP;

                    new Thread(() => ClientHandler.Process(dispatched, remoteEP)).Start();
                } catch (Exception e)
                {
                    Console.WriteLine($"Unknown UDP error. {e.Message} {e.StackTrace}");
                }
            }
        }

        public static long UnixMS()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
        public long creation = Program.UnixMS(); // creation timestamp for an object
        public long lastPacket = Program.UnixMS(); // what was the last packet associated?
    }
}
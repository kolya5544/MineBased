using SecureRemotePassword;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Globalization;
using static MTUDPDispatcher.Program;

namespace MTUDPDispatcher
{
    public class SRPManager
    {
        public enum AuthPhase
        {
            GOT_BYTES_A = 1,
            GOT_BYTES_M = 2,
            SUCCESS_AUTH = 3,
            FAILED_AUTH = 4
        }

        public AuthPhase phase;

        // stage 1
        public string username;
        public byte[] received_Public;
        public SrpServer srp;
        public SrpEphemeral s_emph;
        public RegisteredUser ru;

        // stage 2
        public byte[] salt;
        public byte[] newPublic;

        public static SRPManager GotBytesA(string username, byte[] receivedBytes)
        {
            var manager = new SRPManager();
            var prm = SrpParameters.Create2048<SHA256>();
            manager.srp = new SrpServer(prm);
            manager.received_Public = receivedBytes;

            lock (db.users)
            {
                var user = db.users.FirstOrDefault((z) => z.username == username);
                manager.ru = user;
                manager.salt = user.salt;
                var serverEphemeral = manager.srp.GenerateEphemeral(user.verifier);
                manager.s_emph = serverEphemeral;
                manager.newPublic = serverEphemeral.Public;
            }
            manager.username = username;
            manager.phase = AuthPhase.GOT_BYTES_A;
           
            return manager;
        }

        public static byte[] GotBytesM(byte[] clientVerifier, SRPManager m)
        {
            try
            {
                var serverSession = m.srp.DeriveSession(m.s_emph.Secret, m.received_Public, m.ru.salt, m.username, m.ru.verifier, clientVerifier);
                m.phase = AuthPhase.GOT_BYTES_M;
                return serverSession.Proof;
            } catch
            {
                m.phase = AuthPhase.FAILED_AUTH;
            }
            return null;
        }

        public static byte[] hexBytes(string hexString)
        {
            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        public static string hexify(byte[] v)
        {
            return BitConverter.ToString(v).ToLower().Replace("-", "");
        }
    }
}

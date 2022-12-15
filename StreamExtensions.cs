using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.LLProtocolHandler;

namespace MTUDPDispatcher
{
    public static class StreamExtensions
    {
        // normal types

        public static long readLong(this Stream ms)
        {
            var z = new byte[8];
            ms.Read(z);
            return BitConverter.ToInt64(R(z));
        }
        public static ulong readULong(this Stream ms)
        {
            var z = new byte[8];
            ms.Read(z);
            return BitConverter.ToUInt64(R(z));
        }
        public static int readInt(this Stream ms)
        {
            var z = new byte[4];
            ms.Read(z);
            return BitConverter.ToInt32(R(z));
        }
        public static uint readUInt(this Stream ms)
        {
            var z = new byte[4];
            ms.Read(z);
            return BitConverter.ToUInt32(R(z));
        }
        public static short readShort(this Stream ms)
        {
            var z = new byte[2];
            ms.Read(z);
            return BitConverter.ToInt16(R(z));
        }
        public static ushort readUShort(this Stream ms)
        {
            var z = new byte[2];
            ms.Read(z);
            return BitConverter.ToUInt16(R(z));
        }
        public static bool readBool(this Stream ms)
        {
            var z = new byte[1];
            ms.Read(z);
            return z[0] == 0x01;
        }
        public static string readString(this Stream ms)
        {
            var z = new byte[2];
            ms.Read(z);
            var len = BitConverter.ToUInt16(R(z));
            var buff = new byte[len];
            ms.Read(buff);
            return Encoding.UTF8.GetString(buff);
        }

        public static void writeLong(this Stream ms, long l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeInt(this Stream ms, int l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeUInt(this Stream ms, uint l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeShort(this Stream ms, short l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeUShort(this Stream ms, ushort l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeFloat(this Stream ms, float l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeDouble(this Stream ms, double l)
        {
            var val = BitConverter.GetBytes(l);
            ms.Write(R(val));
        }
        public static void writeString(this Stream ms, string l)
        {
            var bytes = Encoding.UTF8.GetBytes(l);
            var valLen = R(BitConverter.GetBytes((ushort)bytes.Length));
            ms.Write(valLen);
            ms.Write(bytes);
        }
        public static void writeBool(this Stream ms, bool l)
        {
            ms.Write((l ? new byte[] { 0x01 } : new byte[] { 0x00 }));
        }
    }
}

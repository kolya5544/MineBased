using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.LLPacketDispatcher;

namespace MTUDPDispatcher
{
    public class LLPacketDispatcher
    {
        public enum LLPacketType
        {
            TYPE_CONTROL = 0,
            TYPE_ORIGINAL = 1,
            TYPE_SPLIT = 2,
            TYPE_RELIABLE = 3
        }
        public enum LLPacket_Control_Type
        {
            CONTROLTYPE_ACK = 0,
            CONTROLTYPE_SET_PEER_ID = 1,
            CONTROLTYPE_PING = 2,
            CONTROLTYPE_DISCO = 3
        }

        public static bool IsConnectionPacket(Packet p)
        {
            if (p is null) return false;
            if (p.reliable == true && p.pType == LLPacketType.TYPE_ORIGINAL) return true;
            return false;
        }

        public static bool IsReliableReplyRequired(Packet p)
        {
            if (p is null) return false;
            if (p.reliable == true) return true;
            return false;
        }
    }

    public class Packet
    {
        // peer ID
        public ushort peerId;
        // second type (NOT reliable type)
        public byte packetType;
        // channel
        public byte channel;
        public LLPacketType pType
        {
            get
            {
                return (LLPacketType)packetType;
            }
            set
            {
                packetType = (byte)value;
            }
        }
        // is RELIABLE?
        public bool reliable = false;
        public ushort reliable_seqNum = 0;
        // is CONTROL?
        public byte control_type = 0;
        public LLPacket_Control_Type controlType
        {
            get
            {
                return (LLPacket_Control_Type)control_type;
            }
            set
            {
                control_type = (byte)value;
            }
        }
        public ushort control_data = 0;
        // is ORIGINAL?
        // is SPLIT?
        public ushort split_seqNum = 0;
        public ushort split_chunk_count = 0;
        public ushort split_chunk_num = 0;
        // payload
        public byte[] data;

        // original sender
        public IPEndPoint origin;
    }
}

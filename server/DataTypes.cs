using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.LLProtocolHandler;

namespace MTUDPDispatcher
{
    public class DataTypes
    {
        public struct v3s16
        {
            public short x;
            public short y;
            public short z;

            public v3s16(short x, short y, short z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public byte[] Serialize()
            {
                using (var ms = new MemoryStream())
                {
                    ms.writeShort(x);
                    ms.writeShort(y);
                    ms.writeShort(z);
                    return ms.ToArray();
                }
            }
        }

        public struct v3f
        {
            public float x;
            public float y;
            public float z;

            public v3f(float x, float y, float z)
            {
                this.x = x; this.y = y; this.z = z;
            }

            public byte[] Serialize()
            {
                using (var ms = new MemoryStream())
                {
                    ms.writeFloat(x);
                    ms.writeFloat(y);
                    ms.writeFloat(z);
                    return ms.ToArray();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MomBspTools.util
{
    public static class StringUtils
    {
        public static int MakeByteId(string id)
        {
            if (id.Length != 4)
            {
                throw new ArgumentException("String must be exactly 4 characters long");
            }

            byte[] bytes = Encoding.Default.GetBytes(id);
            return (bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0];
        }

        public static string UnmakeByteId(int id)
        {
            byte[] bytes = new byte[] {
                (byte) id,
                (byte) (id >> 8),
                (byte) (id >> 16),
                (byte) (id >> 24)
            };
            return Encoding.Default.GetString(bytes);
        }
    }
}

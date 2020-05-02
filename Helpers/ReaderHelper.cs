using System;
using System.Text;

namespace SteamQuery.Helpers
{
    public static class ReaderHelper
    {
        public static byte ReadByte(this byte[] source, ref int index)
        {
            return source[index++];
        }

        public static short ReadShort(this byte[] source, ref int index)
        {
            return BitConverter.ToInt16(source, (index += 2) - 2); // Might change this later. I find this way cool, different and as same readability as an other way.
        }

        public static long ReadLong(this byte[] source, ref int index)
        {
            return BitConverter.ToUInt32(source, (index += 4) - 4);
        }

        public static float ReadFloat(this byte[] source, ref int index)
        {
            return BitConverter.ToSingle(source, (index += 4) - 4);
        }
        
        public static string ReadString(this byte[] source, ref int index)
        {
            string result = null;
            for (var nextNullIndex = index; nextNullIndex < source.Length; nextNullIndex++)
            {
                if (source[nextNullIndex] == 0)
                {
                    result = Encoding.UTF8.GetString(source, index, nextNullIndex - index);

                    index = nextNullIndex + 1;

                    break;
                }
            }

            return result;
        }
    }
}
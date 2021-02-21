using System;
using System.Text;

namespace SteamQuery.Helpers
{
    internal static class ReaderHelper
    {
        internal static byte ReadByte(this byte[] source, ref int index) => source[index++];

        internal static short ReadShort(this byte[] source, ref int index) => BitConverter.ToInt16(source, (index += 2) - 2); // Might change this later. I find this way cool, different and as same readability as an other way.

        internal static long ReadLong(this byte[] source, ref int index) => BitConverter.ToUInt32(source, (index += 4) - 4);

        internal static float ReadFloat(this byte[] source, ref int index) => BitConverter.ToSingle(source, (index += 4) - 4);

        internal static string ReadString(this byte[] source, ref int index)
        {
            for (var nextNullIndex = index; nextNullIndex < source.Length; nextNullIndex++)
            {
                if (source[nextNullIndex] == 0x00) // 0x00 is the byte value that which is correspond of null character. Strings in Steam queries are null-terminated.
                {
                    var tempIndex = index;
                    index = nextNullIndex + 1;

                    return Encoding.UTF8.GetString(source, tempIndex, nextNullIndex - tempIndex);
                }
            }

            return null;
        }
    }
}
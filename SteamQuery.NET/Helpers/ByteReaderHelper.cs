using System.Text;

namespace SteamQuery.Helpers;

internal static class ByteReaderHelper
{
    internal static int ReadPacketHeader(this byte[] source)
        => BitConverter.ToInt32(source, 0);

    internal static byte ReadPayloadHeader(this byte[] source)
        => source[4];

    internal static byte ReadByte(this byte[] source, ref int index)
        => source[index++];

    internal static short ReadShort(this byte[] source, ref int index)
        => BitConverter.ToInt16(source, (index += 2) - 2);

    internal static int ReadInt(this byte[] source, ref int index)
        => BitConverter.ToInt32(source, (index += 4) - 4);

    internal static ulong ReadUlong(this byte[] source, ref int index)
        => BitConverter.ToUInt64(source, (index += 8) - 8);

    internal static float ReadFloat(this byte[] source, ref int index)
        => BitConverter.ToSingle(source, (index += 4) - 4);

    internal static string ReadString(this byte[] source, ref int index)
    {
        var terminatorIndex = Array.IndexOf<byte>(source, 0x00, index); // 0x00 is the byte value of null terminator. Strings in Steam queries are null-terminated.
        if (terminatorIndex == -1)
        {
            return null;
        }

        var startIndex = index;

        index = terminatorIndex + 1; // Place reader index to right after terminator.

        return Encoding.UTF8.GetString(source, startIndex, terminatorIndex - startIndex);
    }
}
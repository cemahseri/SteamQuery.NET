using System.Text;
using SteamQuery.Enums;
using SteamQuery.Models;

namespace SteamQuery.Helpers;

internal static class ReaderHelper
{
    internal static PacketIdentifier ReadPacketIdentifier(this byte[] source)
        => (PacketIdentifier)BitConverter.ToInt32(source, 0);

    internal static MultiPacketHeader ReadMultiPacketHeader(this byte[] source)
    {
        var index = 9;

        var multiPacketHeader = new MultiPacketHeader
        {
            IsGoldSourceServer = source.ReadInt(ref index) == -1
        };

        index = 4;

        multiPacketHeader.Id = source.ReadInt(ref index);

        if (multiPacketHeader.IsGoldSourceServer)
        {
            var packetInformation = source.ReadByte(ref index);

            multiPacketHeader.TotalPackets = packetInformation & 0b1111; // Reading lower 4 bits.
            multiPacketHeader.PacketNumber = packetInformation >> 4;     // Reading higher 4 bits.
        }
        else
        {
            // Reading most significant bit.
            multiPacketHeader.IsCompressed = ((multiPacketHeader.Id >> 31) & 1) == 1;

            multiPacketHeader.TotalPackets = source.ReadByte(ref index);
            multiPacketHeader.PacketNumber = source.ReadByte(ref index);
            multiPacketHeader.MaximumPacketSize = source.ReadShort(ref index);

            if (multiPacketHeader.IsCompressed)
            {
                multiPacketHeader.UncompressedResponseSize = source.ReadInt(ref index);
                multiPacketHeader.Crc32Checksum = source.ReadInt(ref index);
            }
        }

        return multiPacketHeader;
    }

    internal static PayloadIdentifier ReadRequestPayloadIdentifier(this byte[] source)
        => (PayloadIdentifier)source[0];

    internal static PayloadIdentifier ReadResponsePayloadIdentifier(this byte[] source)
        => (PayloadIdentifier)source[4];
    
    internal static byte ReadByte(this byte[] source, ref int index)
        => source[index++];

    internal static short ReadShort(this byte[] source, ref int index)
        => BitConverter.ToInt16(source, (index += 2) - 2);

    internal static int ReadInt(this byte[] source, ref int index)
        => BitConverter.ToInt32(source, (index += 4) - 4);

    internal static float ReadFloat(this byte[] source, ref int index)
        => BitConverter.ToSingle(source, (index += 4) - 4);

    internal static ulong ReadUlong(this byte[] source, ref int index)
        => BitConverter.ToUInt64(source, (index += 8) - 8);

    internal static string ReadString(this byte[] source, ref int index)
    {
        // 0x00 is the byte value of null terminator. Strings in Steam queries are null-terminated.
        var terminatorIndex = Array.IndexOf<byte>(source, 0x00, index);
        if (terminatorIndex == -1)
        {
            return null;
        }

        var startIndex = index;

        // Place reader index to right after terminator.
        index = terminatorIndex + 1;

        return Encoding.UTF8.GetString(source, startIndex, terminatorIndex - startIndex);
    }
}
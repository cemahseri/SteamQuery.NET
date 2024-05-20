using SteamQuery.Enums;
using SteamQuery.Models;

namespace SteamQuery.Extensions;

internal static class ByteArrayExtensions
{
    internal static PacketIdentifier ReadPacketIdentifier(this byte[] source)
        => (PacketIdentifier)BitConverter.ToInt32(source, 0);

    internal static PayloadIdentifier ReadRequestPayloadIdentifier(this byte[] source)
        => (PayloadIdentifier)source[0];

    internal static PayloadIdentifier ReadResponsePayloadIdentifier(this byte[] source)
        => (PayloadIdentifier)source[4];

    internal static MultiPacketHeader ReadMultiPacketHeader(this byte[] source)
    {
        using var binaryReader = new BinaryReader(new MemoryStream(source));

        binaryReader.BaseStream.Seek(9, SeekOrigin.Begin);

        var multiPacketHeader = new MultiPacketHeader
        {
            IsGoldSourceServer = binaryReader.ReadInt32() == -1
        };

        binaryReader.BaseStream.Seek(4, SeekOrigin.Begin);

        multiPacketHeader.Id = binaryReader.ReadInt32();

        if (multiPacketHeader.IsGoldSourceServer)
        {
            var packetInformation = binaryReader.ReadByte();

            multiPacketHeader.TotalPackets = packetInformation & 0b1111; // Reading lower 4 bits.
            multiPacketHeader.PacketNumber = packetInformation >> 4;     // Reading higher 4 bits.
        }
        else
        {
            // Reading most significant bit.
            multiPacketHeader.IsCompressed = ((multiPacketHeader.Id >> 31) & 1) == 1;

            multiPacketHeader.TotalPackets = binaryReader.ReadByte();
            multiPacketHeader.PacketNumber = binaryReader.ReadByte();
            multiPacketHeader.MaximumPacketSize = binaryReader.ReadInt16();

            if (multiPacketHeader.IsCompressed)
            {
                multiPacketHeader.UncompressedResponseSize = binaryReader.ReadInt32();
                multiPacketHeader.Crc32Checksum = binaryReader.ReadInt32();
            }
        }

        return multiPacketHeader;
    }
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SteamQuery.Enums;
using SteamQuery.Models;

namespace SteamQuery.Extensions;

internal static class ReadOnlySpanExtensions
{
    extension(ReadOnlySpan<byte> source)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PacketIdentifier ReadPacketIdentifier() => (PacketIdentifier)MemoryMarshal.Read<int>(source[..4]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PayloadIdentifier ReadRequestPayloadIdentifier() => (PayloadIdentifier)source[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PayloadIdentifier ReadResponsePayloadIdentifier() => (PayloadIdentifier)source[4];

        internal MultiPacketHeader ReadMultiPacketHeader()
        {
            var isGoldSource = MemoryMarshal.Read<int>(source.Slice(9, 4)) == -1;

            var index = 4;

            var multiPacketHeader = new MultiPacketHeader
            {
                IsGoldSourceServer = isGoldSource,
                Id = MemoryMarshal.Read<int>(source.Slice(index, 4))
            };

            index += 4;

            if (multiPacketHeader.IsGoldSourceServer)
            {
                var packetInformation = source[index];

                multiPacketHeader.TotalPackets = packetInformation & 0b1111; // Reading lower 4 bits.
                multiPacketHeader.PacketNumber = packetInformation >> 4;     // Reading higher 4 bits.
            }
            else
            {
                multiPacketHeader.IsCompressed = ((multiPacketHeader.Id >> 31) & 1) == 1;

                multiPacketHeader.TotalPackets = source[index++];
                multiPacketHeader.PacketNumber = source[index++];

                multiPacketHeader.MaximumPacketSize = MemoryMarshal.Read<short>(source.Slice(index, 2));
                index += 2;

                if (multiPacketHeader.IsCompressed)
                {
                    multiPacketHeader.UncompressedResponseSize = MemoryMarshal.Read<int>(source.Slice(index, 4));
                    index += 4;

                    multiPacketHeader.Crc32Checksum = MemoryMarshal.Read<int>(source.Slice(index, 4));
                }
            }

            return multiPacketHeader;
        }

        internal string ReadNullTerminatedString(ref int index)
        {
            var indexOfNullCharacter = source[index..].IndexOf<byte>(0x00);

            var @string = Encoding.UTF8.GetString(source.Slice(index, indexOfNullCharacter));

            index += indexOfNullCharacter + 1;

            return @string;
        }
    }
}
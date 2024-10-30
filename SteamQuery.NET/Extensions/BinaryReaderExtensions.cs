using System.Buffers;
using System.Text;

namespace SteamQuery.Extensions;

internal static class BinaryReaderExtensions
{
    internal static string ReadNullTerminatedString(this BinaryReader reader)
    {
        // Steam uses a packet size of up to 1400 bytes + IP/UDP headers. If a request or response needs more packets for the data it starts the packets with an additional header.
        // So, if you even assume that the whole packet is a one big null terminated string, you'll be safe using 4 KBs of buffer.
        // But, Rust has been observed breaking this, sending packets up to the maximum IP size - 64KB.
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        var stringLength = buffer.Length;
        for (var i = 0; i < buffer.Length; i++)
        {
            var @byte = reader.ReadByte();
            if (@byte == '\0')
            {
                stringLength = i;
                break;
            }

            buffer[i] = @byte;
        }

        var @string = Encoding.UTF8.GetString(buffer, 0, stringLength);

        ArrayPool<byte>.Shared.Return(buffer, true);

        return @string;
    }
}
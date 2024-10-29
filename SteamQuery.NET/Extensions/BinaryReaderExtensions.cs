using System.Buffers;
using System.Text;

namespace SteamQuery.Extensions;

internal static class BinaryReaderExtensions
{
    internal static string ReadNullTerminatedString(this BinaryReader reader)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1 * 1024);

        for (var i = 0; i < buffer.Length; i++)
        {
            var @byte = reader.ReadByte();
            if (@byte == '\0')
            {
                break;
            }

            buffer[i] = @byte;
        }

        var @string = Encoding.UTF8.GetString(buffer);

        ArrayPool<byte>.Shared.Return(buffer, true);

        return @string;
    }
}
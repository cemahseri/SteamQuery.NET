using System.Text;

namespace SteamQuery.Extensions;

internal static class BinaryReaderExtensions
{
    internal static string ReadNullTerminatedString(this BinaryReader reader)
    {
        var stringBuilder = new StringBuilder();

        char character;
        while ((character = reader.ReadChar()) != 0x00)
        {
            stringBuilder.Append(character);
        }

        return stringBuilder.ToString();
    }
}
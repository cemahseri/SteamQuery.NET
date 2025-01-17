using System.Net;
using SteamQuery.Models;

namespace SteamQuery;

internal sealed class MasterServerResponseReader
{
    internal static IReadOnlyList<MasterServerResponse> ParseResponse(byte[] response)
    {
        var result = new List<MasterServerResponse>();

        using var binaryReader = new BinaryReader(new MemoryStream(response));
        
        var responseHeader = binaryReader.ReadBytes(6);
        if (!responseHeader.SequenceEqual(new byte[]{ 0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x0A }))
        {
            throw new Exception("Response header is not what expected.");
        }

        while (binaryReader.BaseStream.Position != response.Length)
        {
            var ipAddressOctetBytes = binaryReader.ReadBytes(4);
            var port = (ushort)((binaryReader.ReadByte() << 8) + binaryReader.ReadByte());

            if (ipAddressOctetBytes.All(b => b == 0x00) && port == 0)
            {
                break;
            }

            result.Add(new MasterServerResponse(new IPAddress(ipAddressOctetBytes), port));
        }

        return result;
    }
}
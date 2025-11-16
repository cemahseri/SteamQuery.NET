using System.Net;
using SteamQuery.Models;

namespace SteamQuery;

internal static class MasterServerResponseReader
{
    internal static IReadOnlyCollection<MasterServerResponse> ParseResponse(ReadOnlySpan<byte> response)
    {
        if (!response[..6].SequenceEqual((ReadOnlySpan<byte>)[0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x0A]))
        {
            throw new Exception("Response header is not what expected.");
        }
        
        var results = new List<MasterServerResponse>(231);

        var index = 6;
        while (index != response.Length)
        {
            var ipAddressOctetBytes = response.Slice(index, 4);
            index += 4;

            var port = (ushort)((response[index++] << 8) + response[index++]);

            if (ipAddressOctetBytes.IndexOfAnyExcept<byte>(0) == -1 && port == 0)
            {
                break;
            }

            results.Add(new MasterServerResponse(new IPAddress(ipAddressOctetBytes), port));
        }

        return results;
    }
}
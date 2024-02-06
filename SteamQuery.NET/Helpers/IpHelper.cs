using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SteamQuery.Exceptions;

namespace SteamQuery.Helpers;

internal static class IpHelper
{
    internal static IPEndPoint CreateIpEndPoint(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        var parts = endpoint.Split(':');
        if (parts.Length != 2)
        {
            // If result of splitting the endpoint by colon does not return 2 items, it means that endpoint format is wrong.
            // Example 1: "localhost"
            // Example 2: "127.0.0.1:1337:27015"
            throw new FormatException("Invalid endpoint format.");
        }

        // By using NumberStyles.None number style, we do not allow leading or trailing white space, thousands separators, or a decimal separator.
        // It means that the string to be parsed must consist of integral decimal digits only.
        if (!ushort.TryParse(parts.Last(), NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
        {
            throw new InvalidPortException();
        }

        return CreateIpEndPoint(parts.First(), port);
    }

    internal static IPEndPoint CreateIpEndPoint(string hostNameOrIpAddress, int port)
    {
        if (string.IsNullOrEmpty(hostNameOrIpAddress))
        {
            throw new ArgumentNullException(nameof(hostNameOrIpAddress));
        }

        if (port is < ushort.MinValue or > ushort.MaxValue)
        {
            throw new InvalidPortException();
        }

        if (!IPAddress.TryParse(hostNameOrIpAddress, out var ipAddress)) // If it's not a valid IP address, then it might be a hostname like: play.somehostname.com
        {
            ipAddress = Dns.GetHostAddresses(hostNameOrIpAddress).FirstOrDefault();
            
            if (ipAddress == default)
            {
                // Nah, it's not a valid hostname neither.
                // Maybe there is no IP address that's binded with hostname or there is no any IPv4 address in that hostname.
                throw new AddressNotFoundException();
            }
        }

        return new IPEndPoint(ipAddress, port);
    }
}
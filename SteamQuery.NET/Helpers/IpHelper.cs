using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SteamQuery.Exceptions;

namespace SteamQuery.Helpers;

public static class IpHelper
{
    public static IPEndPoint CreateIpEndPoint(string endpoint, AddressFamily addressFamily = AddressFamily.InterNetwork)
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
            // Example 2: "localhost:"
            // Example 3: ":27015"
            // Example 4: "127.0.0.1:1337:27015"
            throw new FormatException("Invalid endpoint format.");
        }

        // By using NumberStyles.None number style, we do not allow leading or trailing white space, thousands separators, or a decimal separator.
        // It means that the string to be parsed must consist of integral decimal digits only.
        if (!ushort.TryParse(parts.Last(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port))
        {
            throw new InvalidPortException();
        }

        return CreateIpEndPoint(parts.First(), port, addressFamily);
    }

    public static IPEndPoint CreateIpEndPoint(string hostNameOrIpAddress, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
        if (string.IsNullOrEmpty(hostNameOrIpAddress))
        {
            throw new ArgumentNullException(nameof(hostNameOrIpAddress));
        }
        
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        // If it's not a valid IP address, then it might be a hostname like: play.somehostname.com
        if (!IPAddress.TryParse(hostNameOrIpAddress, out var ipAddress))
        {
            ipAddress = Dns.GetHostAddresses(hostNameOrIpAddress)
                .FirstOrDefault(ip => ip.AddressFamily == addressFamily);

            if (ipAddress == null)
            {
                // Nah, it's not a valid hostname either. Perhaps there is no IP address that's bound with hostname.
                throw new AddressNotFoundException();
            }
        }

        return new IPEndPoint(ipAddress, port);
    }
}
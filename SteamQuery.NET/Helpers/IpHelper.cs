using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SteamQuery.Exceptions;

namespace SteamQuery.Helpers;

public static class IpHelper
{
    public static IPEndPoint CreateIpEndPoint(ReadOnlySpan<char> endpoint, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
        if (endpoint.IsEmpty)
        {
            throw new ArgumentException("The value cannot be empty.", nameof(endpoint));
        }

        if (endpoint.Count(':') != 1)
        {
            // If result of splitting the endpoint by colon does not return 2 items, it means that endpoint format is wrong.
            // Example 1: "localhost"
            // Example 2: "localhost:"
            // Example 3: ":27015"
            // Example 4: "127.0.0.1:1337:27015"
            throw new FormatException("Invalid endpoint format.");
        }

        var indexOfColon = endpoint.IndexOf(':');

        // By using NumberStyles.None number style, we do not allow leading or trailing white space, thousands separators, or a decimal separator.
        // It means that the string to be parsed must consist of integral decimal digits only.
        if (!ushort.TryParse(endpoint[(indexOfColon + 1)..], NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port))
        {
            throw new InvalidPortException();
        }

        return CreateIpEndPoint(endpoint[..indexOfColon], port, addressFamily);
    }

    public static IPEndPoint CreateIpEndPoint(ReadOnlySpan<char> hostNameOrIpAddress, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
        if (hostNameOrIpAddress.IsEmpty)
        {
            throw new ArgumentException("The value cannot be empty.", nameof(hostNameOrIpAddress));
        }
        
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        // If it's not a valid IP address, then it might be a hostname like: play.hostname.com
        if (!IPAddress.TryParse(hostNameOrIpAddress, out var ipAddress))
        {
            ipAddress = Dns.GetHostAddresses(hostNameOrIpAddress.ToString(), addressFamily).FirstOrDefault();

            if (ipAddress == null)
            {
                // Nah, it's not a valid hostname either. Perhaps there is no IP address that's bound with hostname.
                throw new AddressNotFoundException();
            }
        }

        return new IPEndPoint(ipAddress, port);
    }
}
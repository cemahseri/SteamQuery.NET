using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SteamQuery.Exceptions;

namespace SteamQuery.Helpers;

internal static class IpHelper
{
    internal static IPEndPoint CreateIpEndPoint(string endpoint, AddressFamily addressFamily)
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
        if (!ushort.TryParse(parts.Last(), NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port))
        {
            throw new InvalidPortException();
        }

        return CreateIpEndPoint(parts.First(), port, addressFamily);
    }

    internal static IPEndPoint CreateIpEndPoint(string hostNameOrIpAddress, int port, AddressFamily addressFamily)
    {
        if (string.IsNullOrEmpty(hostNameOrIpAddress))
        {
            throw new ArgumentNullException(nameof(hostNameOrIpAddress));
        }

        if (port is < ushort.MinValue or > ushort.MaxValue)
        {
            throw new InvalidPortException();
        }

        // If it's not a valid IP address, then it might be a hostname like: play.somehostname.com
        if (!IPAddress.TryParse(hostNameOrIpAddress, out var ipAddress))
        {
            try
            {
                // If there is no hostname like that, this will throw: "SocketException: No such host is known"
                // So, instead, throw AddressNotFoundException below, which is much more explainful.
                ipAddress = Dns.GetHostAddresses(hostNameOrIpAddress).FirstOrDefault(ip => ip.AddressFamily == addressFamily);
            }
            catch
            {
            }

            if (ipAddress == default)
            {
                // Nah, it's not a valid hostname either.
                // Maybe there is no IP address that's binded with hostname.
                throw new AddressNotFoundException();
            }
        }

        return new IPEndPoint(ipAddress, port);
    }
}
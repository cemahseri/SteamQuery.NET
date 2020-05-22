using System;
using System.Globalization;
using System.Linq;
using System.Net;
using SteamQuery.Exceptions;

namespace SteamQuery.Helpers
{
    public static class IpHelper
    {
        public static IPEndPoint CreateIpEndPoint(string ip, int port) => CreateIpEndPoint(ip + ":" + port);

        public static IPEndPoint CreateIpEndPoint(string endPoint)
        {
            if (endPoint.Count(c => c == ':') != 1) // If there is no colon or more than one in the input.
            {
                throw new FormatException("Invalid endpoint format.");
            }

            var ep = endPoint.Split(':');

            if(!IPAddress.TryParse(ep[0], out var ip)) // If it's not a valid IP address, then it might be a hostname like: play.somehostname.com
            {
                ip = Dns.GetHostEntry(ep[0]).AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork); // Only IPv4.
                if (ip == null) // Nah, it's not a valid hostname neither. (Or maybe there is no IP address that's binded with hostname - or there is no any IPv4 address in that hostname.)
                {
                    throw new AddressNotFoundException();
                }
            }

            if(!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
            {
                throw new InvalidPortException();
            }

            return new IPEndPoint(ip, port);
        }
    }
}
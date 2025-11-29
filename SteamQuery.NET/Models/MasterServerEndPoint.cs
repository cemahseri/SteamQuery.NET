using System.Net;
using SteamQuery.Helpers;

namespace SteamQuery.Models;

public readonly struct MasterServerEndPoint(string hostNameOrIpAddress, int port)
{
    public static readonly MasterServerEndPoint GoldSrc = new("hl1master.steampowered.com", 27011);
    public static readonly MasterServerEndPoint Source = new("hl2master.steampowered.com", 27011);

    public IPEndPoint IpEndPoint { get; } = IpHelper.CreateIpEndPoint(hostNameOrIpAddress, port);
}
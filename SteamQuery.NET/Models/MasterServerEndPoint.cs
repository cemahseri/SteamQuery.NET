using System.Net;
using SteamQuery.Helpers;

namespace SteamQuery.Models;

public readonly struct MasterServerEndPoint(string hostNameOrIpAddress, int port)
{
    public static readonly MasterServerEndPoint GoldSrc = new("hl1master.steampowered.com", 27011);
    public static readonly MasterServerEndPoint Source = new("hl2master.steampowered.com", 27011);

    private readonly Lazy<IPEndPoint> _lazyIpEndPoint = new(() => IpHelper.CreateIpEndPoint(hostNameOrIpAddress, port));
    public IPEndPoint IpEndPoint => _lazyIpEndPoint.Value;
}
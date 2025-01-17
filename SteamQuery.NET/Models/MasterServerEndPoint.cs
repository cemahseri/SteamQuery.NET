using System.Net;
using SteamQuery.Helpers;

namespace SteamQuery.Models;

public readonly struct MasterServerEndPoint(string hostNameOrIpAddress, int port)
{
    public static readonly MasterServerEndPoint GoldSrc = new("hl1master.steampowered.com", 27011);
    public static readonly MasterServerEndPoint Source = new("hl2master.steampowered.com", 27011);
    //public static readonly MasterServerEndPoint RagDollKungFu = new("", 27015);
    //public static readonly MasterServerEndPoint RedOrchestra = new("", 27015); // Unknown - same as Source?
    //public static readonly MasterServerEndPoint SiN1Multiplayer = new("69.28.151.162", 27010); // Still current?

    public IPEndPoint IpEndPoint { get; } = IpHelper.CreateIpEndPoint(hostNameOrIpAddress, port);
}
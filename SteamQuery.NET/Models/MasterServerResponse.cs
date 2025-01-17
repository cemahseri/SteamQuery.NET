using System.Net;

namespace SteamQuery.Models;

public readonly record struct MasterServerResponse(IPAddress IpAddress, int Port);
namespace SteamQuery.Models;

public sealed record SteamQueryHalfLifeMod
{
    public string Link { get; internal set; }

    public string DownloadLink { get; internal set; }

    public int Version { get; internal set; }

    public int SizeInBytes { get; internal set; }

    public bool IsMultiplayerOnly { get; internal set; }

    public bool HasOwnDll { get; internal set; }
}
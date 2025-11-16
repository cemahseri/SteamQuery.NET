namespace SteamQuery.Models;

public class SteamQueryHalfLifeMod
{
    public string Link { get; internal init; }

    public string DownloadLink { get; internal init; }

    public int Version { get; internal set; }

    public int SizeInBytes { get; internal set; }

    public bool IsMultiplayerOnly { get; internal set; }

    public bool HasOwnDll { get; internal set; }
}
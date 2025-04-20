using System.Text;

namespace SteamQuery.Models;

public sealed class MasterServerQueryFilters
{
    // https://github.com/ValveSoftware/Proton/blob/proton_9.0/lsteamclient/steamworks_sdk_162/isteammatchmaking.h
    //TODO nor
    //TODO nand
    
    public string ServerName { get; set; }

    public bool Dedicated { get; set; }
    
    // Servers using anti-cheat technology like VAC, but potentially others as well.
    public bool Secure { get; set; }
    
    // Servers running the specified modification (e.g. cstrike)
    public string GameDirectory { get; set; }

    public string Map { get; set; }

    public bool OnLinux { get; set; }

    public bool Public { get; set; }

    public bool? Empty { get; set; }

    public bool NotFull { get; set; }
    
    public bool SpectatorProxy { get; set; }

    public int AppId { get; set; }
    public int NotAppId { get; set; }

    public bool Whitelist { get; set; }

    public List<string> GameTypes { get; set; } = [];
    
    public List<string> GameData { get; set; } = [];
    public List<string> GameDataOr { get; set; } = [];

    public string Version { get; set; }

    public bool UniqueIpAddress { get; set; }

    public string IpAddress { get; set; }

    public byte[] GetFilterBytes()
    {
        var stringBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(ServerName))
        {
            stringBuilder.Append(@"\name_match\")
                .Append(ServerName);
        }

        if (Dedicated)
        {
            stringBuilder.Append(@"\dedicated\1");
        }

        if (Secure)
        {
            stringBuilder.Append(@"\secure\1");
        }

        if (!string.IsNullOrEmpty(GameDirectory))
        {
            stringBuilder.Append(@"\gamedir\")
                .Append(GameDirectory);
        }
        
        if (!string.IsNullOrEmpty(Map))
        {
            stringBuilder.Append(@"\map\")
                .Append(Map);
        }

        if (OnLinux)
        {
            stringBuilder.Append(@"\linux\1");
        }

        if (Public)
        {
            stringBuilder.Append(@"\password\0");
        }

        if (Empty == true)
        {
            stringBuilder.Append(@"\noplayers\1");
        }
        else if (Empty == false)
        {
            stringBuilder.Append(@"\empty\1");
        }

        if (NotFull)
        {
            stringBuilder.Append(@"\full\1");
        }

        if (SpectatorProxy)
        {
            stringBuilder.Append(@"\proxy\1");
        }

        if (AppId > 0)
        {
            stringBuilder.Append(@"\appid\")
                .Append(AppId);
        }

        if (NotAppId > 0)
        {
            stringBuilder.Append(@"\napp\")
                .Append(NotAppId);
        }

        if (Whitelist)
        {
            stringBuilder.Append(@"\white\1");
        }

        if (GameTypes.Any())
        {
            stringBuilder.Append(@"\gametype\")
                .Append(string.Join(",", GameTypes));
        }

        if (GameData.Any())
        {
            stringBuilder.Append(@"\gamedata\")
                .Append(string.Join(",", GameTypes));
        }

        if (GameDataOr.Any())
        {
            stringBuilder.Append(@"\gamedataor\")
                .Append(string.Join(",", GameTypes));
        }

        if (!string.IsNullOrEmpty(Version))
        {
            stringBuilder.Append(@"\version_match\")
                .Append(Version);
        }

        if (UniqueIpAddress)
        {
            stringBuilder.Append(@"\collapse_addr_hash\1");
        }

        if (!string.IsNullOrEmpty(IpAddress))
        {
            stringBuilder.Append(@"\gameaddr\")
                .Append(IpAddress);
        }

        stringBuilder.Append('\0');

        return Encoding.UTF8.GetBytes(stringBuilder.ToString());
    }
}
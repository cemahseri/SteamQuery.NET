using SteamQuery.Enums;

namespace SteamQuery.Models;

public record SteamQueryInformation
{
    public byte ProtocolVersion { get; internal set; }

    public string ServerName { get; internal set; }

    public string Map { get; internal set; }

    public string Folder { get; internal set; }

    public string GameName { get; internal set; }

    public short Id { get; internal set; }

    public int OnlinePlayers { get; internal set; }

    public int MaxPlayers { get; internal set; }

    public int Bots { get; internal set; }

    public SteamQueryServerType ServerType { get; internal set; }

    public SteamQueryEnvironment Environment { get; internal set; }

    public bool Visible { get; internal set; }

    public bool VacSecured { get; internal set; }

    // If the GameId is 2400.
    public SteamQueryTheShipGameMode TheShipGameMode { get; internal set; }
    public byte TheShipWitnesses { get; internal set; }
    public byte TheShipDuration { get; internal set; }

    public string Version { get; internal set; }

    // Extra bytes.
    public byte? ExtraDataFlag { get; internal set; }

    public short? Port { get; internal set; }

    public ulong? SteamId { get; internal set; }

    public short? SourceTvPort { get; internal set; }

    public string SourceTvName { get; internal set; }

    public string Keywords { get; internal set; }

    public ulong? GameId { get; internal set; }

    // Obsolete GoldSource properties.
    public bool? IsHalfLifeMod { get; internal set; }

    // If the IsHalfLifeMod is true.
    public SteamQueryHalfLifeMod HalfLifeMod { get; internal set; } = new();
}
using SteamQuery.Enums;

namespace SteamQuery.Models;

public sealed record Informations
{
    public byte Protocol { get; internal set; }

    public string ServerName { get; internal set; }

    public string Map { get; internal set; }

    public string Folder { get; internal set; }

    public string GameName { get; internal set; }

    public short Id { get; internal set; }

    public List<Player> Players { get; } = [];

    public int MaxPlayers => Players.Capacity;

    public int Bots { get; internal set; }

    public ServerType ServerType { get; internal set; }

    public EnvironmentType Environment { get; internal set; }

    public bool Visible { get; internal set; }

    public bool VacSecured { get; internal set; }

    public TheShipGameMode TheShipGameMode { get; internal set; }
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
}
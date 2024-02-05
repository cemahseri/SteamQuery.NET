using SteamQuery.Enums;

namespace SteamQuery.Models;

public sealed record Informations
{
    public byte Protocol { get; set; }

    public string ServerName { get; set; }

    public string Map { get; set; }

    public string Folder { get; set; }

    public string GameName { get; set; }

    public short Id { get; set; }

    public List<Player> Players { get; } = [];

    public int MaxPlayers => Players.Capacity;

    public int Bots { get; set; }

    public ServerType ServerType { get; set; }

    public EnvironmentType Environment { get; set; }

    public bool Visible { get; set; }

    public bool VacSecured { get; set; }

    public string Version { get; set; }

    // Extra bytes.
    public byte? ExtraDataFlag { get; set; }

    public short? Port { get; set; }

    public ulong? SteamId { get; set; }

    public short? SourceTvPort { get; set; }

    public string SourceTvName { get; set; }

    public string Keywords { get; set; }

    public ulong? GameId { get; set; }
}
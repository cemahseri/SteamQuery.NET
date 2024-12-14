using System.Globalization;

namespace SteamQuery.Models;

public record SteamQueryPlayer
{
    public byte Index { get; internal set; }

    public string Name { get; internal set; }

    public long Score { get; internal set; }

    public float DurationSeconds { get; internal set; }
    public TimeSpan DurationTimeSpan => TimeSpan.FromSeconds(DurationSeconds);
    public string Duration => DurationTimeSpan.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    public int TheShipDeaths { get; internal set; }
    public int TheShipMoney { get; internal set; }
}
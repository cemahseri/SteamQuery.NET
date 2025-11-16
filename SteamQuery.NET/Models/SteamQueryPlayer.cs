using System.Globalization;

namespace SteamQuery.Models;

public readonly record struct SteamQueryPlayer(byte Index, string Name, long Score, float DurationSeconds)
{
    public TimeSpan DurationTimeSpan => TimeSpan.FromSeconds(DurationSeconds);
    public string Duration => DurationTimeSpan.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
}
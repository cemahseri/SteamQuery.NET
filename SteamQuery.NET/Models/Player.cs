namespace SteamQuery.Models;

public sealed record Player
{
    public required byte Index { get; init; }

    public required string Name { get; init; }

    public required long Score { get; init; }

    private readonly float _durationSeconds;
    public required float DurationSeconds
    {
        get => _durationSeconds;
        init
        {
            _durationSeconds = value;
            DurationTimeSpan = TimeSpan.FromSeconds(value);
            Duration = DurationTimeSpan.ToString(@"hh\:mm\:ss");
        }
    }
    public TimeSpan DurationTimeSpan { get; init; }
    public string Duration { get; init; }

    public int TheShipDeaths { get; internal set; }
    public int TheShipMoney { get; internal set; }
}
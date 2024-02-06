namespace SteamQuery.Models;

public sealed record Player
{
    public byte Index { get; internal set; }

    public string Name { get; internal set; }

    public long Score { get; internal set; }

    private float _durationSeconds;
    public float DurationSeconds
    {
        get => _durationSeconds;
        internal set
        {
            _durationSeconds = value;
            DurationTimeSpan = TimeSpan.FromSeconds(value);
            Duration = DurationTimeSpan.ToString(@"hh\:mm\:ss");
        }
    }
    public TimeSpan DurationTimeSpan { get; internal set; }
    public string Duration { get; internal set; }

    public int TheShipDeaths { get; internal set; }
    public int TheShipMoney { get; internal set; }
}
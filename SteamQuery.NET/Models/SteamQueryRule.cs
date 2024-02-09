namespace SteamQuery.Models;

public sealed record SteamQueryRule
{
    public string Name { get; internal set; }

    public string Value { get; internal set; }
}
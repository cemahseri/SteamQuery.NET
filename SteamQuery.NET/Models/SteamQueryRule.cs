namespace SteamQuery.Models;

public record SteamQueryRule
{
    public string Name { get; internal set; }

    public string Value { get; internal set; }
}
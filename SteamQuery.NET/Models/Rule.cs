namespace SteamQuery.Models;

public sealed record Rule
{
    public string Name { get; internal set; }

    public string Value { get; internal set; }
}
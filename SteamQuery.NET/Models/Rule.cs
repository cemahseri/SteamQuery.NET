namespace SteamQuery.Models;

public sealed record Rule
{
    public required string Name { get; init; }

    public required string Value { get; init; }
}
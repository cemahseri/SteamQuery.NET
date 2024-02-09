namespace SteamQuery.Enums;

[Flags]
public enum SteamQueryA2SQuery
{
    Information = 1,
    Players     = 1 << 1,
    Rules       = 1 << 2,

    All = Information | Players | Rules
}
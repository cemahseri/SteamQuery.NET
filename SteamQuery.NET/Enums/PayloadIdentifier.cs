namespace SteamQuery.Enums;

public enum PayloadIdentifier : byte
{
    Challenge = 0x41,

    Informations = 0x54,
    Players = 0x55,
    Rules = 0x56,

    GetChallenge = 0x57, // Deprecated
    Ping = 0x69,         // Deprecated

    OldGoldSource = 0x6D
}
namespace SteamQuery.Enums;

public enum PayloadHeader : byte
{
    Challenge = 0x41,

    Informations = 0x54,
    Players = 0x55,
    Rules = 0x56,

    ObsoleteGoldSource = 0x6D
}
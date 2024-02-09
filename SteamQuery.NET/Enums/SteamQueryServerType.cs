namespace SteamQuery.Enums;

public enum SteamQueryServerType
{
    Other,        // 0x00
    Dedicated,    // 0x64 (0x44 for obsolete GoldSource)
    NonDedicated, // 0x6C (0x4C for obsolete GoldSource)
    SourceTv,     // 0x70
    HlTv          // 0x50 (Obsolete GoldSource version of SourceTv)
}
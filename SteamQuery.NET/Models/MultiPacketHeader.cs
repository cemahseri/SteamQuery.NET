namespace SteamQuery.Models;

internal sealed record MultiPacketHeader
{
    internal required bool IsGoldSourceServer { get; init; }

    internal int Id { get; set; }

    internal int TotalPackets { get; set; }

    internal int PacketNumber { get; set; }

    internal short MaximumPacketSize { get; set; }

    internal bool IsCompressed { get; set; }

    internal int? UncompressedResponseSize { get; set; }
    internal int? Crc32Checksum { get; set; }
}
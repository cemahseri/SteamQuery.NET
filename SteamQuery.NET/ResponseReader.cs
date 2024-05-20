using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Extensions;
using SteamQuery.Models;

namespace SteamQuery;

internal static class ResponseReader
{
    internal static SteamQueryInformation ParseInformation(byte[] response)
    {
        return response.ReadResponsePayloadIdentifier() == PayloadIdentifier.ObsoleteGoldSource
            ? ParseObsoleteInformationPayload(response)
            : ParseStandardInformationPayload(response);
    }

    private static SteamQueryInformation ParseObsoleteInformationPayload(byte[] response)
    {
        var information = new SteamQueryInformation();

        using var binaryReader = new BinaryReader(new MemoryStream(response));

        // First 4 bytes are 0xFF packet header prefixes and the other byte is payload identifier.
        // Payload identifiers are always the same and we do not need them. So, skip first 5 bytes.
        binaryReader.BaseStream.Seek(5, SeekOrigin.Begin);

        // This is the address property. We do not really need the server's IP address and port, do we?
        binaryReader.ReadNullTerminatedString();

        information.ServerName = binaryReader.ReadNullTerminatedString();
        information.Map = binaryReader.ReadNullTerminatedString();
        information.Folder = binaryReader.ReadNullTerminatedString();
        information.GameName = binaryReader.ReadNullTerminatedString();

        information.OnlinePlayers = binaryReader.ReadByte();
        information.MaxPlayers = binaryReader.ReadByte();

        information.ProtocolVersion = binaryReader.ReadByte();

        information.ServerType = binaryReader.ReadByte() switch
        {
            0x44 => SteamQueryServerType.Dedicated,
            0x4C => SteamQueryServerType.NonDedicated,
            0x50 => SteamQueryServerType.HlTv,
            _ => SteamQueryServerType.Other
        };

        var environment = binaryReader.ReadByte();
        information.Environment = environment switch
        {
            0x4C or 0x6C => SteamQueryEnvironment.Linux,
            0x57 or 0x77 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x4C, 0x57 ])
        };

        information.Visible = !binaryReader.ReadBoolean();

        information.IsHalfLifeMod = binaryReader.ReadBoolean();

        if (information.IsHalfLifeMod == true)
        {
            var halfLifeMod = new SteamQueryHalfLifeMod
            {
                Link = binaryReader.ReadNullTerminatedString(),
                DownloadLink = binaryReader.ReadNullTerminatedString()
            };

            // This does exist and equals to 0x00 for some reason...
            binaryReader.ReadByte();

            halfLifeMod.Version = binaryReader.ReadInt32();
            halfLifeMod.SizeInBytes = binaryReader.ReadInt32();
            halfLifeMod.IsMultiplayerOnly = binaryReader.ReadBoolean();
            halfLifeMod.HasOwnDll = binaryReader.ReadBoolean();

            information.HalfLifeMod = halfLifeMod;
        }

        information.VacSecured = binaryReader.ReadBoolean();

        information.Bots = binaryReader.ReadByte();

        return information;
    }

    private static SteamQueryInformation ParseStandardInformationPayload(byte[] response)
    {
        var information = new SteamQueryInformation();

        using var binaryReader = new BinaryReader(new MemoryStream(response));

        binaryReader.BaseStream.Seek(5, SeekOrigin.Begin);

        information.ProtocolVersion = binaryReader.ReadByte();
        information.ServerName = binaryReader.ReadNullTerminatedString();
        information.Map = binaryReader.ReadNullTerminatedString();
        information.Folder = binaryReader.ReadNullTerminatedString();
        information.GameName = binaryReader.ReadNullTerminatedString();
        information.Id = binaryReader.ReadInt16();

        information.OnlinePlayers = binaryReader.ReadByte();
        information.MaxPlayers = binaryReader.ReadByte();
        information.Bots = binaryReader.ReadByte();

        information.ServerType = binaryReader.ReadByte() switch
        {
            0x64 => SteamQueryServerType.Dedicated,
            0x6C => SteamQueryServerType.NonDedicated,
            0x70 => SteamQueryServerType.SourceTv,
            _ => SteamQueryServerType.Other
        };

        var environment = binaryReader.ReadByte();
        information.Environment = environment switch
        {
            0x6C => SteamQueryEnvironment.Linux,
            0x6D => SteamQueryEnvironment.Mac,
            0x6F => SteamQueryEnvironment.Mac,
            0x77 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x6C, 0x6D, 0x6F, 0x77 ])
        };

        information.Visible = !binaryReader.ReadBoolean();
        information.VacSecured = binaryReader.ReadBoolean();

        // 2400 is The Ship: Murder Party's application ID in Steam.
        if (information.GameId == 2400)
        {
            information.TheShipGameMode = (SteamQueryTheShipGameMode)binaryReader.ReadByte();
            information.TheShipWitnesses = binaryReader.ReadByte();
            information.TheShipDuration = binaryReader.ReadByte();
        }

        information.Version = binaryReader.ReadNullTerminatedString();

        // If we have the extra flags.
        if (response.Length - binaryReader.BaseStream.Position > 0)
        {
            information.ExtraDataFlag = binaryReader.ReadByte();

            if ((information.ExtraDataFlag & 0x80) == 0x80)
            {
                information.Port = binaryReader.ReadInt16();
            }

            if ((information.ExtraDataFlag & 0x10) == 0x10)
            {
                information.SteamId = binaryReader.ReadUInt64();
            }

            if ((information.ExtraDataFlag & 0x40) == 0x40)
            {
                information.SourceTvPort = binaryReader.ReadInt16();
                information.SourceTvName = binaryReader.ReadNullTerminatedString();
            }

            if ((information.ExtraDataFlag & 0x20) == 0x20)
            {
                information.Keywords = binaryReader.ReadNullTerminatedString();
            }

            if ((information.ExtraDataFlag & 0x01) == 0x01)
            {
                information.GameId = binaryReader.ReadUInt64();
            }
        }

        return information;
    }

    internal static List<SteamQueryPlayer> ParsePlayers(byte[] response)
    {
        var players = new List<SteamQueryPlayer>();

        using var binaryReader = new BinaryReader(new MemoryStream(response));

        binaryReader.BaseStream.Seek(5, SeekOrigin.Begin);

        var playerCount = binaryReader.ReadByte();
        for (var i = 0; i < playerCount; i++)
        {
            players.Add(new SteamQueryPlayer
            {
                Index = binaryReader.ReadByte(),
                Name = binaryReader.ReadNullTerminatedString(),
                Score = binaryReader.ReadInt32(),
                DurationSeconds = binaryReader.ReadSingle()
            });
        }

        if (binaryReader.BaseStream.Position < response.Length)
        {
            foreach (var player in players)
            {
                player.TheShipDeaths = binaryReader.ReadInt32();
                player.TheShipMoney = binaryReader.ReadInt32();
            }
        }

        return players;
    }

    internal static List<SteamQueryRule> ParseRules(byte[] response)
    {
        var rules = new List<SteamQueryRule>();

        using var binaryReader = new BinaryReader(new MemoryStream(response));

        binaryReader.BaseStream.Seek(5, SeekOrigin.Begin);

        var ruleCount = binaryReader.ReadInt16();
        for (var i = 0; i < ruleCount; i++)
        {
            rules.Add(new SteamQueryRule
            {
                Name = binaryReader.ReadNullTerminatedString(),
                Value = binaryReader.ReadNullTerminatedString()
            });
        }

        return rules;
    }
}
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

internal static class ResponseReader
{
    //TODO Use BinaryReader instead of manually reading them. (Thanks to @Trojaner.)
    internal static SteamQueryInformation ParseInformation(byte[] response)
    {
        return response.ReadResponsePayloadIdentifier() == PayloadIdentifier.ObsoleteGoldSource
            ? ParseObsoleteInformationPayload(response)
            : ParseStandardInformationPayload(response);
    }

    private static SteamQueryInformation ParseObsoleteInformationPayload(byte[] response)
    {
        var information = new SteamQueryInformation();

        // First 4 bytes are 0xFF packet header prefixes and the other byte is payload identifier.
        // Payload identifiers are always the same and we do not need them. So, skip first 5 bytes.
        var index = 5;

        // This is the address property. We do not really need the server's IP address and port, do we?
        response.ReadString(ref index);

        information.ServerName = response.ReadString(ref index);
        information.Map = response.ReadString(ref index);
        information.Folder = response.ReadString(ref index);
        information.GameName = response.ReadString(ref index);

        information.OnlinePlayers = response.ReadByte(ref index);
        information.MaxPlayers = response.ReadByte(ref index);

        information.ProtocolVersion = response.ReadByte(ref index);

        information.ServerType = response.ReadByte(ref index) switch
        {
            0x44 => SteamQueryServerType.Dedicated,
            0x4C => SteamQueryServerType.NonDedicated,
            0x50 => SteamQueryServerType.HlTv,
            _ => SteamQueryServerType.Other
        };

        var environment = response.ReadByte(ref index);
        information.Environment = environment switch
        {
            0x4C or 0x6C => SteamQueryEnvironment.Linux,
            0x57 or 0x77 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x4C, 0x57 ])
        };

        information.Visible = response.ReadByte(ref index) == 0x00;

        information.IsHalfLifeMod = response.ReadByte(ref index) == 0x01;

        if (information.IsHalfLifeMod == true)
        {
            var halfLifeMod = new SteamQueryHalfLifeMod
            {
                Link = response.ReadString(ref index),
                DownloadLink = response.ReadString(ref index)
            };

            // This does exist and equals to 0x00 for some reason...
            response.ReadByte(ref index);

            halfLifeMod.Version = response.ReadInt(ref index);
            halfLifeMod.SizeInBytes = response.ReadInt(ref index);
            halfLifeMod.IsMultiplayerOnly = response.ReadByte(ref index) == 0x01;
            halfLifeMod.HasOwnDll = response.ReadByte(ref index) == 0x01;

            information.HalfLifeMod = halfLifeMod;
        }

        information.VacSecured = response.ReadByte(ref index) == 0x01;

        information.Bots = response.ReadByte(ref index);

        return information;
    }

    private static SteamQueryInformation ParseStandardInformationPayload(byte[] response)
    {
        var information = new SteamQueryInformation();

        var index = 5;

        information.ProtocolVersion = response.ReadByte(ref index);
        information.ServerName = response.ReadString(ref index);
        information.Map = response.ReadString(ref index);
        information.Folder = response.ReadString(ref index);
        information.GameName = response.ReadString(ref index);
        information.Id = response.ReadShort(ref index);

        information.OnlinePlayers = response.ReadByte(ref index);
        information.MaxPlayers = response.ReadByte(ref index);
        information.Bots = response.ReadByte(ref index);

        information.ServerType = response.ReadByte(ref index) switch
        {
            0x64 => SteamQueryServerType.Dedicated,
            0x6C => SteamQueryServerType.NonDedicated,
            0x70 => SteamQueryServerType.SourceTv,
            _ => SteamQueryServerType.Other
        };

        var environment = response.ReadByte(ref index);
        information.Environment = environment switch
        {
            0x6C => SteamQueryEnvironment.Linux,
            0x6D => SteamQueryEnvironment.Mac,
            0x6F => SteamQueryEnvironment.Mac,
            0x77 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x6C, 0x6D, 0x6F, 0x77 ])
        };

        information.Visible = response.ReadByte(ref index) == 0x00;
        information.VacSecured = response.ReadByte(ref index) == 0x01;

        // 2400 is The Ship: Murder Party's application ID in Steam.
        if (information.GameId == 2400)
        {
            information.TheShipGameMode = (SteamQueryTheShipGameMode)response.ReadByte(ref index);
            information.TheShipWitnesses = response.ReadByte(ref index);
            information.TheShipDuration = response.ReadByte(ref index);
        }

        information.Version = response.ReadString(ref index);

        // If we have the extra flags.
        if (response.Length - index > 0)
        {
            information.ExtraDataFlag = response.ReadByte(ref index);

            if ((information.ExtraDataFlag & 0x80) == 0x80)
            {
                information.Port = response.ReadShort(ref index);
            }

            if ((information.ExtraDataFlag & 0x10) == 0x10)
            {
                information.SteamId = response.ReadUlong(ref index);
            }

            if ((information.ExtraDataFlag & 0x40) == 0x40)
            {
                information.SourceTvPort = response.ReadShort(ref index);
                information.SourceTvName = response.ReadString(ref index);
            }

            if ((information.ExtraDataFlag & 0x20) == 0x20)
            {
                information.Keywords = response.ReadString(ref index);
            }

            if ((information.ExtraDataFlag & 0x01) == 0x01)
            {
                information.GameId = response.ReadUlong(ref index);
            }
        }

        return information;
    }

    internal static List<SteamQueryPlayer> ParsePlayers(byte[] response)
    {
        var players = new List<SteamQueryPlayer>();

        var index = 5;

        var playerCount = response.ReadByte(ref index);
        for (var i = 0; i < playerCount; i++)
        {
            players.Add(new SteamQueryPlayer
            {
                Index = response.ReadByte(ref index),
                Name = response.ReadString(ref index),
                Score = response.ReadInt(ref index),
                DurationSeconds = response.ReadFloat(ref index)
            });
        }

        if (index < response.Length)
        {
            foreach (var player in players)
            {
                player.TheShipDeaths = response.ReadInt(ref index);
                player.TheShipMoney = response.ReadInt(ref index);
            }
        }

        return players;
    }

    internal static List<SteamQueryRule> ParseRules(byte[] response)
    {
        var rules = new List<SteamQueryRule>();

        var index = 5;

        var ruleCount = response.ReadShort(ref index);
        for (var i = 0; i < ruleCount; i++)
        {
            rules.Add(new SteamQueryRule
            {
                Name = response.ReadString(ref index),
                Value = response.ReadString(ref index)
            });
        }

        return rules;
    }
}
using System.Runtime.InteropServices;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Extensions;
using SteamQuery.Models;

namespace SteamQuery;

internal static class ServerQueryResponseReader
{
    internal static SteamQueryInformation ParseInformation(ReadOnlySpan<byte> response)
    {
        return response.ReadResponsePayloadIdentifier() == PayloadIdentifier.ObsoleteGoldSource
            ? ParseObsoleteInformationPayload(response)
            : ParseStandardInformationPayload(response);
    }

    private static SteamQueryInformation ParseObsoleteInformationPayload(ReadOnlySpan<byte> response)
    {
        var information = new SteamQueryInformation();

        // First 4 bytes are 0xFF packet header prefixes and the other byte is payload identifier.
        // We do not need them. So, skip first 5 bytes.
        var index = 0x5;

        // This is the address property. We do not really need the server's IP address and port, do we? DO WE?!
        // So just skip it.
        index += response[index..].IndexOf<byte>(0x00) + 0x1;

        information.ServerName = response.ReadNullTerminatedString(ref index);
        information.Map = response.ReadNullTerminatedString(ref index);
        information.Folder = response.ReadNullTerminatedString(ref index);
        information.GameName = response.ReadNullTerminatedString(ref index);

        information.OnlinePlayers = response[index++];
        information.MaxPlayers = response[index++];

        information.ProtocolVersion = response[index++];

        information.ServerType = response[index++] switch
        {
            0x44 or 0x64 => SteamQueryServerType.Dedicated,
            0x4C or 0x6C => SteamQueryServerType.NonDedicated,
            0x50 or 0x70 => SteamQueryServerType.HlTv,
            _ => SteamQueryServerType.Other
        };

        var environment = response[index++];
        information.Environment = environment switch
        {
            0x4C or 0x6C => SteamQueryEnvironment.Linux,
            0x57 or 0x77 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x4C, 0x57 ])
        };

        information.Visible = response[index++] == 0x0;

        information.IsHalfLifeMod = response[index++] == 0x1;

        if (information.IsHalfLifeMod == true)
        {
            var halfLifeMod = new SteamQueryHalfLifeMod
            {
                Link = response.ReadNullTerminatedString(ref index),
                DownloadLink = response.ReadNullTerminatedString(ref index)
            };

            // There is a null byte for some reason...
            index++;

            halfLifeMod.Version = MemoryMarshal.Read<int>(response.Slice(index, 4));
            index += 0x4;

            halfLifeMod.SizeInBytes = MemoryMarshal.Read<int>(response.Slice(index, 4));
            index += 0x4;

            halfLifeMod.IsMultiplayerOnly = response[index] == 0x1;
            halfLifeMod.HasOwnDll = response[index] == 0x1;

            information.HalfLifeMod = halfLifeMod;
        }

        information.VacSecured = response[index++] == 0x1;

        information.Bots = response[index];

        return information;
    }

    private static SteamQueryInformation ParseStandardInformationPayload(ReadOnlySpan<byte> response)
    {
        var information = new SteamQueryInformation();

        var index = 5;

        information.ProtocolVersion = response[index++];
        information.ServerName = response.ReadNullTerminatedString(ref index);
        information.Map = response.ReadNullTerminatedString(ref index);
        information.Folder = response.ReadNullTerminatedString(ref index);
        information.GameName = response.ReadNullTerminatedString(ref index);
        information.Id = MemoryMarshal.Read<short>(response.Slice(index, 2));
        index += 2;

        information.OnlinePlayers = response[index++];
        information.MaxPlayers = response[index++];
        information.Bots = response[index++];

        information.ServerType = response[index++] switch
        {
            0x44 or 0x64 => SteamQueryServerType.Dedicated,
            0x4C or 0x6C => SteamQueryServerType.NonDedicated,
            0x50 or 0x70 => SteamQueryServerType.SourceTv,
            _ => SteamQueryServerType.Other
        };

        var environment = response[index++];
        information.Environment = environment switch
        {
            0x6C or 0x4C => SteamQueryEnvironment.Linux,
            0x4D or 0x6D => SteamQueryEnvironment.Mac,
            0x4F or 0x6F => SteamQueryEnvironment.Mac,
            0x77 or 0x57 => SteamQueryEnvironment.Windows,
            _ => throw new UnexpectedByteException(environment, [ 0x6C, 0x6D, 0x6F, 0x77 ])
        };
        
        information.Visible = response[index++] == 0;
        information.VacSecured = response[index++] == 1;

        // 2400 is The Ship: Murder Party's application ID in Steam.
        if (information.GameId == 2400)
        {
            information.TheShipGameMode = (SteamQueryTheShipGameMode)response[index++];
            information.TheShipWitnesses = response[index++];
            information.TheShipDuration = response[index++];
        }

        information.Version = response.ReadNullTerminatedString(ref index);

        // If we have the extra flags.
        if (response.Length > index)
        {
            information.ExtraDataFlag = response[index++];

            if ((information.ExtraDataFlag & 0x80) == 0x80)
            {
                information.Port = MemoryMarshal.Read<short>(response.Slice(index, 2));
                index += 2;
            }

            if ((information.ExtraDataFlag & 0x10) == 0x10)
            {
                information.SteamId = MemoryMarshal.Read<ulong>(response.Slice(index, 8));
                index += 8;
            }

            if ((information.ExtraDataFlag & 0x40) == 0x40)
            {
                information.SourceTvPort = MemoryMarshal.Read<short>(response.Slice(index, 2));
                index += 2;
                information.SourceTvName = response.ReadNullTerminatedString(ref index);
            }

            if ((information.ExtraDataFlag & 0x20) == 0x20)
            {
                information.Keywords = response.ReadNullTerminatedString(ref index);
            }

            if ((information.ExtraDataFlag & 0x01) == 0x01)
            {
                information.GameId = MemoryMarshal.Read<ulong>(response.Slice(index, 8));
            }
        }

        return information;
    }

    internal static SteamQueryPlayer[] ParsePlayers(ReadOnlySpan<byte> response)
    {
        var index = 5;

        var playerCount = response[index++];

        var players = new SteamQueryPlayer[playerCount];
        for (var i = 0; i < playerCount; i++)
        {
            var playerIndex = response[index++];
            var name = response.ReadNullTerminatedString(ref index);

            var score = MemoryMarshal.Read<int>(response.Slice(index, 4));
            index += 4;

            var durationSeconds = MemoryMarshal.Read<float>(response.Slice(index, 4));
            index += 4;

            players[i] = new SteamQueryPlayer(playerIndex, name, score, durationSeconds);
        }

        return players;
    }

    internal static SteamQueryRule[] ParseRules(ReadOnlySpan<byte> response)
    {
        var index = 5;

        var ruleCount = MemoryMarshal.Read<short>(response.Slice(index, 2));
        index += 2;
        
        var rules = new SteamQueryRule[ruleCount];
        for (var i = 0; i < ruleCount; i++)
        {
            var name = response.ReadNullTerminatedString(ref index);
            var value = response.ReadNullTerminatedString(ref index);

            rules[i] = new SteamQueryRule(name, value);
        }

        return rules;
    }
}
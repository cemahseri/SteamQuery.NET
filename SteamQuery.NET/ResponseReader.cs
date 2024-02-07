using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

public static class ResponseReader
{
    public static Information ParseInformation(byte[] response)
    {
        var information = new Information();

        var index = 5; // First 4 bytes are FF prefixes and the other byte is header. Headers are always the same and we do not need them, so skip first 5 bytes.

        information.Protocol = response.ReadByte(ref index);
        information.ServerName = response.ReadString(ref index);
        information.Map = response.ReadString(ref index);
        information.Folder = response.ReadString(ref index);
        information.GameName = response.ReadString(ref index);
        information.Id = response.ReadShort(ref index);

        var playerCount = response.ReadByte(ref index);
        information.Players.Capacity = response.ReadByte(ref index);

        for (var i = 0; i < playerCount; i++)
        {
            information.Players.Add(null); // Adding null instead of new object is faster and uses less memory - and also will help while garbage collecting, I guess.
        }

        information.Bots = response.ReadByte(ref index);

        var serverType = response.ReadByte(ref index);
        information.ServerType = serverType switch
        {
            0x00 => ServerType.RagDollKungFu,
            0x64 => ServerType.Dedicated,
            0x6C => ServerType.NonDedicated,
            0x70 => ServerType.SourceTv,
            _ => throw new UnexpectedByteException(serverType, 0x00, 0x64, 0x6C, 0x70)
        };

        var environment = response.ReadByte(ref index);
        information.Environment = environment switch
        {
            0x6C => EnvironmentType.Linux,
            0x6D => EnvironmentType.Mac,
            0x6F => EnvironmentType.Mac,
            0x77 => EnvironmentType.Windows,
            _ => throw new UnexpectedByteException(environment, 0x6C, 0x6D, 0x6F, 0x77)
        };

        information.Visible = response.ReadByte(ref index) == 0x00;
        information.VacSecured = response.ReadByte(ref index) == 0x01;

        if (information.GameId == 2400) // 2400 is The Ship: Murder Party's application ID in Steam.
        {
            information.TheShipGameMode = (TheShipGameMode)response.ReadByte(ref index);
            information.TheShipWitnesses = response.ReadByte(ref index);
            information.TheShipDuration = response.ReadByte(ref index);
        }

        information.Version = response.ReadString(ref index);

        if (response.Length - index > 0) // If we have the extra flags.
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

    public static List<Player> ParsePlayers(byte[] response)
    {
        var players = new List<Player>();

        var index = 5;

        var playerCount = response.ReadByte(ref index);
        for (var i = 0; i < playerCount; i++)
        {
            players.Add(new Player
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

    public static List<Rule> ParseRules(byte[] response)
    {
        var rules = new List<Rule>();

        var index = 5;

        var ruleCount = response.ReadShort(ref index);
        for (var i = 0; i < ruleCount; i++)
        {
            rules.Add(new Rule
            {
                Name = response.ReadString(ref index),
                Value = response.ReadString(ref index)
            });
        }

        return rules;
    }
}
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Helpers;
using SteamQuery.Models;

namespace SteamQuery;

public static class ResponseReader
{
    public static Informations ParseInformation(byte[] response)
    {
        var informations = new Informations();

        var index = 5; // First 4 bytes are FF prefixes and the other byte is header. Headers are always the same and we do not need them, so skip first 5 bytes.

        informations.Protocol = response.ReadByte(ref index);
        informations.ServerName = response.ReadString(ref index);
        informations.Map = response.ReadString(ref index);
        informations.Folder = response.ReadString(ref index);
        informations.GameName = response.ReadString(ref index);
        informations.Id = response.ReadShort(ref index);

        var playerCount = response.ReadByte(ref index);
        informations.Players.Capacity = response.ReadByte(ref index);

        for (var i = 0; i < playerCount; i++)
        {
            informations.Players.Add(null); // Adding null instead of new object is faster and uses less memory - and also will help while garbage collecting, I guess.
        }

        informations.Bots = response.ReadByte(ref index);

        var serverType = response.ReadByte(ref index);
        informations.ServerType = serverType switch
        {
            0x00 => ServerType.RagDollKungFu,
            0x64 => ServerType.Dedicated,
            0x6C => ServerType.NonDedicated,
            0x70 => ServerType.SourceTv,
            _ => throw new UnexpectedByteException(serverType, 0x00, 0x64, 0x6C, 0x70)
        };

        var environment = response.ReadByte(ref index);
        informations.Environment = environment switch
        {
            0x6C => EnvironmentType.Linux,
            0x6D => EnvironmentType.Mac,
            0x6F => EnvironmentType.Mac,
            0x77 => EnvironmentType.Windows,
            _ => throw new UnexpectedByteException(environment, 0x6C, 0x6D, 0x6F, 0x77)
        };

        informations.Visible = response.ReadByte(ref index) == 0x00;
        informations.VacSecured = response.ReadByte(ref index) == 0x01;

        if (informations.GameId == 2400) // 2400 is The Ship: Murder Party's application ID in Steam.
        {
            informations.TheShipGameMode = (TheShipGameMode)response.ReadByte(ref index);
            informations.TheShipWitnesses = response.ReadByte(ref index);
            informations.TheShipDuration = response.ReadByte(ref index);
        }

        informations.Version = response.ReadString(ref index);

        if (response.Length - index > 0) // If we have the extra flags.
        {
            informations.ExtraDataFlag = response.ReadByte(ref index);

            if ((informations.ExtraDataFlag & 0x80) == 0x80)
            {
                informations.Port = response.ReadShort(ref index);
            }

            if ((informations.ExtraDataFlag & 0x10) == 0x10)
            {
                informations.SteamId = response.ReadUlong(ref index);
            }

            if ((informations.ExtraDataFlag & 0x40) == 0x40)
            {
                informations.SourceTvPort = response.ReadShort(ref index);
                informations.SourceTvName = response.ReadString(ref index);
            }

            if ((informations.ExtraDataFlag & 0x20) == 0x20)
            {
                informations.Keywords = response.ReadString(ref index);
            }

            if ((informations.ExtraDataFlag & 0x01) == 0x01)
            {
                informations.GameId = response.ReadUlong(ref index);
            }
        }

        return informations;
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
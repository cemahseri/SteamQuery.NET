using System.Collections.Generic;
using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Models;

namespace SteamQuery.Helpers
{
    public static class ResponseParseHelper
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
            for (var i = 0; i < playerCount; i++)
            {
                informations.Players.Add(null); // Adding null instead of new object is faster and uses less memory - and also will help while garbage collecting, I guess.
            }

            informations.MaxPlayers = response.ReadByte(ref index);
            informations.Bots = response.ReadByte(ref index);

            var serverType = response.ReadByte(ref index);
            informations.ServerType = serverType switch
            {
                0x64 => ServerType.Dedicated,
                0x6c => ServerType.NonDedicated,
                0x00 => ServerType.NonDedicated,
                0x70 => ServerType.SourceTv,
                _ => throw new UnexpectedByteException(serverType, 0x00, 0x64, 0x6c, 0x70)
            };
            
            var environment = response.ReadByte(ref index);
            informations.Environment = environment switch
            {
                0x77 => EnvironmentType.Windows,
                0x6c => EnvironmentType.Linux,
                0x6d => EnvironmentType.Mac,
                0x6f => EnvironmentType.Mac,
                _ => throw new UnexpectedByteException(environment, 0x6c, 0x6d, 0x6f, 0x77)
            };
            
            informations.Visible = response.ReadByte(ref index) == 0x00;
            informations.VacSecured = response.ReadByte(ref index) == 0x01;
            // The Ship is missing here for now. Will add later.
            informations.Version = response.ReadString(ref index);

            if (response.Length - index > 0) // We have the extra flags.
            {
                informations.ExtraDataFlag = response.ReadByte(ref index);

                if ((informations.ExtraDataFlag & 0x80) == 1)
                {
                    informations.Port = response.ReadShort(ref index);
                }

                if ((informations.ExtraDataFlag & 0x10) == 1)
                {
                    informations.SteamId = response.ReadLong(ref index);
                }

                if ((informations.ExtraDataFlag & 0x40) == 1)
                {
                    informations.SourceTvPort = response.ReadShort(ref index);
                    informations.SourceTvName = response.ReadString(ref index);
                }

                if ((informations.ExtraDataFlag & 0x20) == 1)
                {
                    informations.Keywords = response.ReadString(ref index);
                }

                if ((informations.ExtraDataFlag & 0x01) == 1)
                {
                    informations.GameId = response.ReadLong(ref index);
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
                    Score = response.ReadLong(ref index),
                    DurationSecond = response.ReadFloat(ref index)
                });
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
}
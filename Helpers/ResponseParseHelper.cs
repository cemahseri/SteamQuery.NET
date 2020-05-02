using System.Collections.Generic;
using SteamQuery.Enums;
using SteamQuery.Models;

namespace SteamQuery.Helpers
{
    public static class ResponseParseHelper
    {
        public static Informations ParseInformation(Informations informations, byte[] response)
        {
            var index = 5; // First 4 bytes are FF prefixes and the other byte is header. Headers are always the same and we do not need them, so skip first 5 bytes.
            
            informations.Protocol = response.ReadByte(ref index);
            informations.ServerName = response.ReadString(ref index);
            informations.Map = response.ReadString(ref index);
            informations.Folder = response.ReadString(ref index);
            informations.GameName = response.ReadString(ref index);
            informations.Id = response.ReadShort(ref index);
            index++; // Skipping informations.Players list here because we are going to get detailed informations of players, not only just count of them.
            informations.MaxPlayers = response.ReadByte(ref index);
            informations.Bots = response.ReadByte(ref index);

            informations.ServerType = response.ReadByte(ref index) switch
            {
                0x64 => ServerType.Dedicated,
                0x6c => ServerType.NonDedicated,
                0x00 => ServerType.NonDedicated,
                0x70 => ServerType.SourceTv,
                _ => informations.ServerType
            };
            
            informations.Environment = response.ReadByte(ref index) switch
            {
                0x77 => EnvironmentType.Windows,
                0x6c => EnvironmentType.Linux,
                0x6d => EnvironmentType.Mac,
                0x6f => EnvironmentType.Mac,
                _ => informations.Environment
            };
            
            informations.Visible = response.ReadByte(ref index) == 0x00;
            informations.VacSecured = response.ReadByte(ref index) == 0x01;
            // The Ship is missing here for now. Will add later.
            informations.Version = response.ReadString(ref index);

            if (response.Length - index > 0)
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

        public static List<Player> ParsePlayers(List<Player> players, byte[] response)
        {
            var index = 5;

            var playerCount = response.ReadByte(ref index);
            for (var i = 0; i < playerCount; i++)
            {
                players.Add(new Player
                {
                    Index = response.ReadByte(ref index),
                    Name = response.ReadString(ref index),
                    Score = response.ReadLong(ref index),
                    Duration = response.ReadFloat(ref index)
                });
            }

            return players;
        }

        public static List<Rule> ParseRules(List<Rule> rules, byte[] response)
        {
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
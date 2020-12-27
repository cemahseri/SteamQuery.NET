using SteamQuery.Helpers;
using SteamQuery.Models;
using SteamQuery.Utils;
using System;
using System.Collections.Generic;

namespace SteamQuery.Parsers
{
    internal class PlayerlistParser : QueryParserBase<List<Player>, PlayerlistParser>
    {
        internal override List<Player> Parse(byte[] input)
        {
            var players = new List<Player>();

            var index = 5;
            var playerCount = input.ReadByte(ref index);
            for (var i = 0; i < playerCount; i++)
            {
                players.Add(new Player
                {
                    Index = input.ReadByte(ref index),
                    Name = input.ReadString(ref index),
                    Score = input.ReadLong(ref index),
                    DurationSecond = input.ReadFloat(ref index)
                });
            }
            return players;
        }
    }
}

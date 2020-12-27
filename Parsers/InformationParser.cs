using SteamQuery.Enums;
using SteamQuery.Exceptions;
using SteamQuery.Models;
using SteamQuery.Utils;

namespace SteamQuery.Parsers
{
    internal class InformationParser : QueryParserBase<Informations, InformationParser>
    {
        internal override Informations Parse(byte[] input)
        {
            var informations = new Informations();

            var index = 5; // First 4 bytes are FF prefixes and the other byte is header. Headers are always the same and we do not need them, so skip first 5 bytes.

            informations.Protocol = input.ReadByte(ref index);
            informations.ServerName = input.ReadString(ref index);
            informations.Map = input.ReadString(ref index);
            informations.Folder = input.ReadString(ref index);
            informations.GameName = input.ReadString(ref index);
            informations.Id = input.ReadShort(ref index);

            var playerCount = input.ReadByte(ref index);
            for (var i = 0; i < playerCount; i++)
            {
                informations.Players.Add(null); // Adding null instead of new object is faster and uses less memory - and also will help while garbage collecting, I guess.
            }

            informations.MaxPlayers = input.ReadByte(ref index);
            informations.Bots = input.ReadByte(ref index);

            var serverType = input.ReadByte(ref index);
            informations.ServerType = serverType switch
            {
                0x64 => ServerType.Dedicated,
                0x6c => ServerType.NonDedicated,
                0x00 => ServerType.NonDedicated,
                0x70 => ServerType.SourceTv,
                _ => throw new UnexpectedByteException(serverType, 0x00, 0x64, 0x6c, 0x70)
            };

            var environment = input.ReadByte(ref index);
            informations.Environment = environment switch
            {
                0x77 => EnvironmentType.Windows,
                0x6c => EnvironmentType.Linux,
                0x6d => EnvironmentType.Mac,
                0x6f => EnvironmentType.Mac,
                _ => throw new UnexpectedByteException(environment, 0x6c, 0x6d, 0x6f, 0x77)
            };

            informations.Visible = input.ReadByte(ref index) == 0x00;
            informations.VacSecured = input.ReadByte(ref index) == 0x01;
            // The Ship is missing here for now. Will add later.
            informations.Version = input.ReadString(ref index);

            if (input.Length - index > 0) // If we have the extra flags.
            {
                informations.ExtraDataFlag = input.ReadByte(ref index);

                if ((informations.ExtraDataFlag & 0x80) == 1)
                {
                    informations.Port = input.ReadShort(ref index);
                }

                if ((informations.ExtraDataFlag & 0x10) == 1)
                {
                    informations.SteamId = input.ReadLong(ref index);
                }

                if ((informations.ExtraDataFlag & 0x40) == 1)
                {
                    informations.SourceTvPort = input.ReadShort(ref index);
                    informations.SourceTvName = input.ReadString(ref index);
                }

                if ((informations.ExtraDataFlag & 0x20) == 1)
                {
                    informations.Keywords = input.ReadString(ref index);
                }

                if ((informations.ExtraDataFlag & 0x01) == 1)
                {
                    informations.GameId = input.ReadLong(ref index);
                }
            }
            return informations;
        }
    }
}

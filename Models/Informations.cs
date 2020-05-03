using System.Collections.Generic;
using SteamQuery.Enums;

namespace SteamQuery.Models
{
    public sealed class Informations
    {
        public byte Protocol { get; set; }

        public string ServerName { get; set; }

        public string Map { get; set; }

        public string Folder { get; set; }

        public string GameName { get; set; }

        public short Id { get; set; }
        
        public int MaxPlayers { get; set; }
        
        public List<Player> Players { get; set; } = new List<Player>();

        public int Bots { get; set; }

        public ServerType ServerType { get; set; }

        public EnvironmentType Environment { get; set; }

        public bool Visible { get; set; }

        public bool VacSecured { get; set; }

        public string Version { get; set; }

        // Extra bytes.
        public byte? ExtraDataFlag { get; set; }

        public short? Port { get; set; }

        public long? SteamId { get; set; }

        public long? GameId { get; set; }

        public short? SourceTvPort { get; set; }

        public string SourceTvName { get; set; }

        public string Keywords { get; set; }
    }
}
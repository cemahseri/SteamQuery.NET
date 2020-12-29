using System.Text.Json.Serialization;

namespace SteamQuery.Models
{
    public class MasterlistServer
    {
        [JsonPropertyName("addr")]
        public string Address { get; set; }
        [JsonPropertyName("gameport")]
        public ushort Gameport { get; set; }
        [JsonPropertyName("steamid")]
        public string SteamId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("appid")]
        public ulong Appid { get; set; }
        [JsonPropertyName("gamedir")]
        public string Gamedir { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("product")]
        public string Product { get; set; }
        [JsonPropertyName("region")]
        public sbyte Region { get; set; }
        [JsonPropertyName("players")]
        public long Players { get; set; }
        [JsonPropertyName("max_players")]
        public long MaxPlayers { get; set; }
        [JsonPropertyName("bots")]
        public long Bots { get; set; }
        [JsonPropertyName("map")]
        public string Map { get; set; }
        [JsonPropertyName("secure")]
        public bool IsSecure { get; set; }
        [JsonPropertyName("dedicated")]
        public bool IsDedicated { get; set; }
        [JsonPropertyName("os")]
        public string OS { get; set; }
        [JsonPropertyName("gametype")]
        public string Gametype { get; set; }
    }
}

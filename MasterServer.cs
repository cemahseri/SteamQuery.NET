using SteamQuery.Enums;
using SteamQuery.Models;
using SteamQuery.Parsers;
using SteamQuery.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SteamQuery
{
    public class MasterServer
    {
        private readonly string key;
        private readonly WebClient webClient;
        public MasterServer(string key, WebClient webClient)
        {
            this.key = key;
            this.webClient = webClient;
        }
        public MasterlistServer[] GetServers(MasterlistFilter masterlistFilter, ulong limit)
        {
            string url = $"https://api.steampowered.com/IGameServersService/GetServerList/v1/?limit={limit}&key={key}&filter={MasterlistUtils.BuildFilter(masterlistFilter)}";
            var json = webClient.DownloadString(url);
            return JsonSerializer.Deserialize<MasterlistResponse>(json).Response.Servers;
        }
    }
}

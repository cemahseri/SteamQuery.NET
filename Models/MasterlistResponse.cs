using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamQuery.Models
{
    public class MasterlistResponse
    {
        [JsonPropertyName("response")]
        public Response Response { get; set; }
    }
    public class Response
    {
        [JsonPropertyName("servers")]
        public MasterlistServer[] Servers { get; set; }
    }
}

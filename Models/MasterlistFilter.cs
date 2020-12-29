using System;
using System.Collections.Generic;
using System.Text;

namespace SteamQuery.Models
{
    public class MasterlistFilter
    {
        /// <summary>
        /// Servers running dedicated. 
        /// </summary>
        public bool IsDedicated { get; set; }
        /// <summary>
        /// Servers using anti-cheat technology.(eg:-VAC)
        /// </summary>
        public bool IsSecure { get; set; }
        /// <summary>
        /// Servers running the specified modification.(ex. cstrike) 
        /// </summary>
        public string GameDirectory { get; set; }
        /// <summary>
        /// Servers running the specified map. 
        /// </summary>
        public string Map { get; set; }
        /// <summary>
        /// Servers running on a Linux platform. 
        /// </summary>
        public bool IsLinux { get; set; }
        /// <summary>
        /// Servers that are not empty. 
        /// </summary>
        public bool IsNotEmpty { get; set; }
        /// <summary>
        /// Servers that are not full. 
        /// </summary>
        public bool IsNotFull { get; set; }
        /// <summary>
        /// Servers that are spectator proxies. 
        /// </summary>
        public bool IsProxy { get; set; }
        /// <summary>
        /// Servers that are NOT running a game(AppId)(This was introduced to block Left 4 Dead games from the Steam Server Browser).
        /// </summary>
        public ulong NAppId { get; set; }
        /// <summary>
        /// Servers that are empty. 
        /// </summary>
        public bool IsNoPlayers { get; set; }
        /// <summary>
        /// Servers that are whitelisted. 
        /// </summary>
        public bool IsWhiteListed { get; set; }
        /// <summary>
        /// Servers with all of the given tag(s) in sv_tags(separated by comma). 
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Servers with all of the given tag(s) in their 'hidden' tags (L4D2)(separated by comma). 
        /// </summary>
        public string HiddenTagsAll { get; set; }
        /// <summary>
        /// Servers with any of the given tag(s) in their 'hidden' tags (L4D2)(separated by comma). 
        /// </summary>
        public string HiddenTagsAny { get; set; }
        /// <summary>
        /// Servers that are running game that has mentioned Application Id.
        /// </summary>
        public ulong AppId { get; set; }
        /// <summary>
        /// Servers with the mentioned hostname.
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// Servers running mentioned version.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Return only one server for each unique IP address matched.
        /// </summary>
        public bool IsUniqueIPAddress { get; set; }
        /// <summary>
        /// Return only servers on the specified End Point(Port is optional).
        /// </summary>
        public string IPAddress { get; set; }
        /// <summary>
        /// Exclude servers that match any of the mentioned conditions(Nor filter).
        /// </summary>
        public MasterlistFilter ExcludeAny { get; set; }
        /// <summary>
        /// Exclude servers that match all of the mentioned conditions(Nand filter).
        /// </summary>
        public MasterlistFilter ExcludeAll { get; set; }
    }
}

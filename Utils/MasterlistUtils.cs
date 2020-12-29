using SteamQuery.Enums;
using SteamQuery.Models;
using System;
using System.Linq;
using System.Text;

namespace SteamQuery.Utils
{
    public static class MasterlistUtils
    {
        //Depracated, use SteamWebApi instead.

        //public static byte[] BuildPayload(string endpoint, RegionCode region, string masterlistFilter)
        //{
        //    byte[] bEndpoint = endpoint.StringToSzArr();
        //    byte[] bFilter = !string.IsNullOrEmpty(masterlistFilter) ? masterlistFilter.StringToSzArr() : new byte[] {0x00};
        //    byte[] bPayload = new byte[bEndpoint.Length + bFilter.Length + 2];

        //    bPayload[0] = 0x31;
        //    bPayload[1] = (byte)region;
        //    Array.Copy(bEndpoint, 0, bPayload, 2, bEndpoint.Length);
        //    Array.Copy(bFilter, 0, bPayload, bEndpoint.Length + 2, bFilter.Length);
        //    return bPayload;
        //}
        public static string BuildFilter(MasterlistFilter filter, bool isSubFilter = false)
        {
            StringBuilder filterStr = new StringBuilder();

            if (filter.IsDedicated)
                filterStr.Append(@"\type\d");
            if (filter.IsSecure)
                filterStr.Append(@"\secure\1");
            if (!string.IsNullOrEmpty(filter.GameDirectory))
                filterStr.Append(@"\gamedir\" + filter.GameDirectory);
            if (!string.IsNullOrEmpty(filter.Map))
                filterStr.Append(@"\map\" + filter.Map);
            if (filter.IsLinux)
                filterStr.Append(@"\linux\1");
            if (filter.IsNotEmpty)
                filterStr.Append(@"\empty\1");
            if (filter.IsNotFull)
                filterStr.Append(@"\full\1");
            if (filter.IsProxy)
                filterStr.Append(@"\proxy\1");
            if (filter.NAppId != 0)
                filterStr.Append(@"\napp\" + filter.NAppId);
            if (filter.IsNoPlayers)
                filterStr.Append(@"\noplayers\1");
            if (filter.IsWhiteListed)
                filterStr.Append(@"\white\1");
            if (!string.IsNullOrEmpty(filter.Tags))
                filterStr.Append(@"\gametype\" + filter.Tags);
            if (!string.IsNullOrEmpty(filter.HiddenTagsAll))
                filterStr.Append(@"\gamedata\" + filter.HiddenTagsAll);
            if (!string.IsNullOrEmpty(filter.HiddenTagsAny))
                filterStr.Append(@"\gamedataor\" + filter.HiddenTagsAny);
            if (filter.AppId != 0)
                filterStr.Append(@"\appid\" + filter.AppId);
            if (!string.IsNullOrEmpty(filter.HostName))
                filterStr.Append(@"\name_match\" + filter.HostName);
            if (!string.IsNullOrEmpty(filter.Version))
                filterStr.Append(@"\version_match\" + filter.Version);
            if (filter.IsUniqueIPAddress)
                filterStr.Append(@"\collapse_addr_hash\1");
            if (!string.IsNullOrEmpty(filter.IPAddress))
                filterStr.Append(@"\gameaddr\" + filter.IPAddress);
            if (filter.ExcludeAny != null)
            {
                filterStr.Append("\0nor");
                filterStr.Append(BuildFilter(filter.ExcludeAny, true));
            }
            if (filter.ExcludeAll != null)
            {
                filterStr.Append("\0nand");
                filterStr.Append(BuildFilter(filter.ExcludeAll, true));
            }
            if (!isSubFilter)
            {
                string[] Parts = null;
                string norStr = string.Empty, nandStr = string.Empty;
                Parts = filterStr.ToString().Split('\0');
                filterStr = new StringBuilder(Parts[0]);
                for (int i = 1; i < Parts.Length; i++)
                {
                    if (Parts[i].StartsWith("nor", StringComparison.OrdinalIgnoreCase))
                    {
                        norStr += Parts[i].Substring(3);
                    }
                    if (Parts[i].StartsWith("nand", StringComparison.OrdinalIgnoreCase))
                    {
                        nandStr += Parts[i].Substring(4);
                    }
                }
                if (!string.IsNullOrEmpty(norStr))
                {
                    filterStr.Append(@"\nor\");
                    filterStr.Append(norStr.Count(x => x == '\\') / 2);
                    filterStr.Append(norStr);
                }
                if (!string.IsNullOrEmpty(nandStr))
                {
                    filterStr.Append(@"\nand\");
                    filterStr.Append(nandStr.Count(x => x == '\\') / 2);
                    filterStr.Append(nandStr);
                }
            }
            return filterStr.ToString();
        } 
    }
}

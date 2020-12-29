using System;
using System.Collections.Generic;
using System.Text;

namespace SteamQuery.Enums
{
   public enum RegionCode : byte
   {
        US_EAST_COAST = 0x00,
        US_WEST_COAST = 0x01,
        SOUTH_AMERICA = 0x02,
        EUROPE = 0x03,
        ASIA = 0x04,
        AUSTRALIA = 0x05,
        MIDDLE_EAST = 0x06,
        AFRICA = 0x07,
        WORLD = 0xFF
   }
}

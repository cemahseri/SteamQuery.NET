//Deprecated, use SteamWebApi instead.


//using System.Net;
//using System.Text;

//namespace SteamQuery.Parsers
//{
//    class MasterlistParser : QueryParserBase<string[], MasterlistParser>
//    {
//        internal override string[] Parse(byte[] input)
//        {
//            Span<byte> s = input;
//            var index = 6;
//            List<string> addresses = new List<string>();
//            byte[] address = new byte[4];
//            ushort port;
//            //Calculates how many addresses are in one block.
//            for (int i = 0; i < (input.Length - 6) / 6; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    address[j] = ReaderUtils.ReadByte(input, ref index);
//                }
//                //Converts to LittleEndian
//                var portBytes = s.Slice((index += 2) - 2, 2);
//                portBytes.Reverse();
//                port = BitConverter.ToUInt16(portBytes);

//                addresses.Add($"{address[0]}.{address[1]}.{address[2]}.{address[3]}:{port}");
//            }
//            return addresses.ToArray();
//        }
//    }
//}

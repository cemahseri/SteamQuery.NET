using SteamQuery.Helpers;

namespace SteamQuery.Parsers
{
    internal abstract class QueryParserBase<ParserReturnType, ChildClass> : SingletonHelper<ChildClass> where ChildClass : class
    {
        internal abstract ParserReturnType Parse(byte[] input);
    }
}

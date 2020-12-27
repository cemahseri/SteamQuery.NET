using SteamQuery.Models;
using SteamQuery.Utils;
using System.Collections.Generic;

namespace SteamQuery.Parsers
{
    internal class RuleParser : QueryParserBase<List<Rule>, RuleParser>
    {
        internal override List<Rule> Parse(byte[] input)
        {
            var rules = new List<Rule>();
            var index = 5;

            var ruleCount = input.ReadShort(ref index);
            for (var i = 0; i < ruleCount; i++)
            {
                rules.Add(new Rule
                {
                    Name = input.ReadString(ref index),
                    Value = input.ReadString(ref index)
                });
            }
            return rules;
        }
    }
}

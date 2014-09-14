using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;
using NLog;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Analyzers
{
    [Analyzer("Regex", true)]
    [Description("Break up a string using a regex. Each group will become strings. The group name will be used as the element name.")]
    [Parameter("Regex", typeof(string), "The regex to use")]
    [Serializable]
    public class RegexAnalyzer : Analyzer
    {
        public string Regex { get; set; }

        public RegexAnalyzer()
        {
        }

        public RegexAnalyzer(Dictionary<string, Variant> args)
        {
            ParameterParser.Parse(this, args);
        }

        public override void asDataElement(Peach.Core.Dom.DataElement parent, Dictionary<Peach.Core.Dom.DataElement, Peach.Core.Cracker.Position> positions)
        {
            // Verify the parent type is a string.
            if (!(parent is Peach.Core.Dom.String))
                throw new SoftException("Error, Regex analyzer can only be used with String elements. Element '" + parent.fullName + "' is a '" + parent.elementType + "'.");

            var regex = new System.Text.RegularExpressions.Regex(Regex);

            var data = (string)parent.DefaultValue;
            var match = regex.Match(data);
            if (!match.Success)
                throw new SoftException("The Regex analyzer failed to match.");

            var sorted = new SortedDictionary<int, Peach.Core.Dom.String>();

            // Create the Block element that will contain the matched strings
            var block = new Block(parent.name);

            // The order of groups does not always match order from string
            // we will add them into a sorted dictionary to order them correctly
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                var str = new Peach.Core.Dom.String(regex.GroupNameFromNumber(i));
                str.DefaultValue = new Variant(group.Value);
                sorted[group.Index] = str;
            }

            // Add elements in order they appeared in string
            foreach (var item in sorted.Keys)
                block.Add(sorted[item]);

            // Replace our current element (String) with the Block of matched strings
            parent.parent[parent.name] = block;
        }
    }
}

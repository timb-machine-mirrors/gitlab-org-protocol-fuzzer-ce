using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PeachFarm.Common
{
	public static class CommonExtensions
	{
		public static List<string> ReverseFormatString(this string str, string template)
		{
			string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

			Regex r = new Regex(pattern);
			Match m = r.Match(str);

			List<string> ret = new List<string>();

			for (int i = 1; i < m.Groups.Count; i++)
			{
				ret.Add(m.Groups[i].Value);
			}

			return ret;
		}

		private static bool HasAny<T>(IEnumerable<T> collection)
		{
			return collection != null && collection.Any();
		}
	}
}

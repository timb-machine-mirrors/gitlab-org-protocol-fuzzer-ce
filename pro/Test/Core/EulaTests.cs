using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.License;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class EulaTests
	{
		[Datapoints]
		public string[] versions = Enum.GetNames(typeof(LicenseFeature));

		[Theory]
		public void HaveEulaText(string version)
		{
			LicenseFeature ver;
 			if (!Enum.TryParse(version, out ver))
				Assert.Fail("Enumeration value '{0}' is not a valid License.Version".Fmt(version));

			var license = new PortableLicense();
			var txt = license.EulaText(ver);

			Assert.NotNull(txt);
			Assert.Greater(txt.Length, 0);
		}

		// This code is used for converting HTML EULAs into text.
		static void FixEulaText(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: MakeEula.exe <infile.txt> <outfile.txt>");
				return;
			}

			var input = File.ReadAllLines(args[0], Encoding.UTF8);
			var output = new List<string>();

			var whitespace = new Regex("\\s+");
			var number = new Regex("^(\\d+\\.)\\s+(.*?)\\r?$");
			var letter = new Regex("^(\\([a-z]\\))\\s+(.*?)\\r?$");
			var special = new Regex("[“”’™–]");

			var indent = "";

			Func<string, string, string> fmt = (s, i) => string.IsNullOrEmpty(s) ? s : i + s;

			Func<string, int, List<string>> lineSplit = (src, count) =>
			{
				var ret = new List<string>();

				var start = 0;
				var end = src.Length;

				while (end - start > count)
				{
					var pos = start + count;

					while (pos > start)
					{
						if (src[pos] == ' ')
							break;
						--pos;
					}

					ret.Add(src.Substring(start, pos - start));
					start = ++pos;
				}

				ret.Add(src.Substring(start, end - start));

				return ret;
			};


			foreach (var raw in input)
			{
				// Replace all whitespace with a single space
				var l = whitespace.Replace(raw, " ").Trim();

				// Eat consecutive empty lines
				if (l.Length == 0 && string.IsNullOrEmpty(output.LastOrDefault()))
					continue;

				l = special.Replace(l, m =>
				{
					var ret = m.ToString();

					switch (ret)
					{
						case "“":
						case "”":
							return "\"";
						case "’":
							return "'";
						case "™":
							return "(TM)";
						case "–":
							return "-";
					}

					return ret;
				});

				if (output.Count == 0)
					l = l.ToUpper();

				var isNum = number.Match(l);
				if (isNum.Success)
				{
					if (!string.IsNullOrEmpty(output.Last()))
						output.Add("");

					indent = "    ";

					var h = isNum.Groups[1].Value;

					h += new string(' ', 4 - h.Length);

					var lines = lineSplit(isNum.Groups[2].Value, 80 - 4);

					output.Add(h + lines[0]);
					output.AddRange(lines.Skip(1).Select(m => fmt(m, indent)));

					continue;
				}

				var isLetter = letter.Match(l);
				if (isLetter.Success)
				{
					indent = "    ";

					var h = isLetter.Groups[1].Value;

					h += indent.Substring(h.Length);

					var lines = lineSplit(isLetter.Groups[2].Value, 80 - h.Length);

					output.Add(h + lines[0]);
					output.AddRange(lines.Skip(1).Select(m => fmt(m, indent)));

					continue;
				}

				if (l.StartsWith("BY ACCESSING OR USING THE SOFTWARE"))
					indent = "";

				var split = lineSplit(l, 80 - indent.Length);
				output.AddRange(split.Select(m => fmt(m, indent)));
			}

			while (string.IsNullOrEmpty(output[output.Count - 1]))
				output.RemoveAt(output.Count - 1);

			var enc = Encoding.GetEncoding("us-ascii", new EncoderExceptionFallback(), new DecoderExceptionFallback());

			foreach (var l in output)
			{
				try
				{
					var b = enc.GetBytes(l);
					Assert.NotNull(b);
				}
				catch (EncoderFallbackException ex)
				{
					Console.WriteLine("Non ascii character in line:");
					Console.WriteLine(l);
					Console.WriteLine(ex.Message);
					return;
				}
			}

			File.WriteAllLines(args[1], output, enc);
		}

	}
}

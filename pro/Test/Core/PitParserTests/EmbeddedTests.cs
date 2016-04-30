﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class EmbeddedTests
	{
		const string PitsResourcePrefix = "Peach.Pro.Test.Core.Resources.Pits";

		[Test]
		public void BasicTest()
		{
			var asm = Assembly.GetExecutingAssembly();
			var pitName = PitsResourcePrefix + ".Net.DNP3_Slave.xml";
			var configName = PitsResourcePrefix + ".Net.DNP3_Slave.xml.config";

			using (var tmpDir = new TempDirectory())
			{
				ExtractFile(tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
				ExtractDirectory(tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

				PitDefines defs;
				using (var stream = asm.GetManifestResourceStream(configName))
				{
					Assert.NotNull(stream, configName);
					defs = PitDefines.Parse(stream, tmpDir.Path, new Dictionary<string, string> {
						{"Source", "0"},
						{"Destination", "0"},
					});
				}

				var args = new Dictionary<string, object> {
					{ PitParser.DEFINED_VALUES, defs.Evaluate() }
				};

				var parser = new ProPitParser(tmpDir.Path, asm, PitsResourcePrefix);
				Peach.Core.Dom.Dom dom;
				using (var stream = asm.GetManifestResourceStream(pitName))
				using (var reader = new StreamReader(stream))
				{
					Assert.NotNull(stream, pitName);
					dom = parser.asParser(args, reader, pitName, true);
				}

				var config = new RunConfiguration() { singleIteration = true, };

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
		}

		private static void ExtractDirectory(string targetDir, params string[] parts)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(parts));
			Directory.CreateDirectory(dir);

			var asm = Assembly.GetExecutingAssembly();
			var prefix = string.Join(".", new[] { PitsResourcePrefix }.Concat(parts));
			foreach (var name in asm.GetManifestResourceNames())
			{
				if (name.StartsWith(prefix))
				{
					var fileName = name.Substring(prefix.Length + 1); // exclude last '.'
					var target = Path.Combine(dir, fileName);
					Utilities.ExtractEmbeddedResource(asm, name, target);
				}
			}
		}

		private static void ExtractFile(string targetDir, string filename, params string[] dirs)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(dirs));
			Directory.CreateDirectory(dir);

			var asm = Assembly.GetExecutingAssembly();
			var name = string.Join(".", 
				new[] { PitsResourcePrefix }
				.Concat(dirs)
				.Concat(new[] { filename })
			);
			var target = Path.Combine(dir, filename);
			Utilities.ExtractEmbeddedResource(asm, name, target);
		}
	}
}

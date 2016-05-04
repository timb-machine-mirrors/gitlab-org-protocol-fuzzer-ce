using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
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
		static readonly Assembly _asm = Assembly.GetExecutingAssembly();
		const string PitsResourcePrefix = "Peach.Pro.Test.Core.Resources.Pits";

		public class PitManifest : Dictionary<string, PitManifestEntry> { }

		public class PitManifestEntry
		{
			public string Pit { get; set; }
			public string[] Assets { get; set; }
		}

		[Test]
		public void BasicTest()
		{

			using (var tmpDir = new TempDirectory())
			{
				ExtractFile(tmpDir.Path, "DNP3_Slave.xml", "Net");
				ExtractFile(tmpDir.Path, "DNP3_Slave.xml.config", "Net");
				ExtractFile(tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
				ExtractDirectory(tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

				var pitFile = Path.Combine(tmpDir.Path, "Net", "DNP3_Slave.xml");
				var pitConfigFile = pitFile + ".config";

				var defs = PitDefines.ParseFile(pitConfigFile, tmpDir.Path, new Dictionary<string, string> {
					{"Source", "0"},
					{"Destination", "0"},
				});

				var args = new Dictionary<string, object> {
					{ PitParser.DEFINED_VALUES, defs.Evaluate() }
				};

				var parser = new ProPitParser(tmpDir.Path, _asm, PitsResourcePrefix);
				var dom = parser.asParser(args, pitFile);
				var config = new RunConfiguration() { singleIteration = true, };
				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
		}

		[Test]
		public void ParseManifest()
		{
			using (var stream = _asm.GetManifestResourceStream(PitsResourcePrefix + ".manifest.json"))
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				var manifest = JsonUtilities.CreateSerializer().Deserialize<PitManifest>(json);
				CollectionAssert.Contains(manifest.Keys, "PeachPit-Net-DNP3_Slave");
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

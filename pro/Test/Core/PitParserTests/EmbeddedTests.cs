using System;
using System.Collections.Generic;
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
		const string LibraryName = "test.peachfuzzer.com";
		const string PitLibraryPath = "asm://" + LibraryName;
		const string PitsResourcePrefix = "Peach.Pro.Test.Core.Resources.Pits";

		[ResourceLoader(LibraryName)]
		public class TestResourceLoader : ResourceLoader
		{
			public TestResourceLoader()
				: base(Assembly.GetExecutingAssembly(), PitsResourcePrefix)
			{ }
		}

		[PythonMetaPathImporter(LibraryName)]
		public class TestResourceMetaPathImporter : ResourceMetaPathImporter
		{
			public TestResourceMetaPathImporter(Uri baseUri)
				: base(baseUri, Assembly.GetExecutingAssembly(), PitsResourcePrefix)
			{ }
		}

		[Test]
		public void BasicTest()
		{
			var configName = "/Net/DNP3_Slave.config.xml";
			var pitName = "/Net/DNP3_Slave.xml";

			using (var tmpDir = new TempDirectory())
			{
				ExtractSamples(tmpDir);

				var type = ClassLoader.FindPluginByName<ResourceLoaderAttribute>(LibraryName);
				Assert.NotNull(type);
				var loader = (IResourceLoader)Activator.CreateInstance(type);

				PitDefines defs;
				using (var stream = loader.GetResource(configName))
				{
					Assert.NotNull(stream);
					defs = PitDefines.Parse(stream, PitLibraryPath, new Dictionary<string, string> {
						{"Source", "0"},
						{"Destination", "0"},
						{"PitSamplesPath", tmpDir.Path},
					});
				}

				var args = new Dictionary<string, object> {
					{ PitParser.DEFINED_VALUES, defs.Evaluate() }
				};

				var parser = new PitParser();
				Peach.Core.Dom.Dom dom;
				using (var stream = loader.GetResource(pitName))
				using (var reader = new StreamReader(stream))
				{
					Assert.NotNull(stream);
					dom = parser.asParser(args, reader, pitName, true);
				}

				var config = new RunConfiguration() { singleIteration = true, };

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
		}

		private static void ExtractSamples(TempDirectory tmpDir)
		{
			var dirs = new[] { "_Common", "Samples", "Net", "DNP3" };
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { tmpDir.Path }.Concat(dirs));
			Directory.CreateDirectory(dir);

			var asm = Assembly.GetExecutingAssembly();
			var prefix = string.Join(".", new[] { PitsResourcePrefix }.Concat(dirs));
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
	}
}

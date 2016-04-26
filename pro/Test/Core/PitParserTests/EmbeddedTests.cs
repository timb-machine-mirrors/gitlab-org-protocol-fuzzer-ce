using System;
using System.Collections.Generic;
using System.IO;
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
		[Test]
		public void BasicTest()
		{
			var asm = Assembly.GetExecutingAssembly();
			var uri = new UriBuilder("asm:Net.DNP3_Master.xml");
			var fileName = Path.GetFileNameWithoutExtension(uri.Path);

			var pitsNamespace = "Peach.Pro.Test.Core.Resources.Pits";
			var pitResName = string.Join(".", pitsNamespace, uri.Path);
			var configResName = string.Join(".", pitsNamespace, fileName, "config", "xml");

			PitDefines defs;
			using (var stream = asm.GetManifestResourceStream(configResName))
			{
				Assert.NotNull(stream);
				defs = PitDefines.Parse(stream, null);
			}

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defs.Evaluate() }
			};

			var parser = new ProPitParser(asm, pitsNamespace);
			Peach.Core.Dom.Dom dom;
			using (var stream = asm.GetManifestResourceStream(pitResName))
			using (var reader = new StreamReader(stream))
			{
				Assert.NotNull(stream);
				dom = parser.asParser(args, reader, fileName, true);
			}

			var config = new RunConfiguration()
			{
				singleIteration = true
			};
		
			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}
	}
}

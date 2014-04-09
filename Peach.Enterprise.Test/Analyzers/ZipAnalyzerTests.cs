using System;
using System.IO;
using NUnit.Framework;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;
using Peach.Core;
using Ionic.Zip;

namespace Peach.Enterprise.Test.Analyzers
{
	[TestFixture]
	class ZipAnalyzerTests
	{
		[Test]
		public void Crack1()
		{
			var bs = new BitStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("foo", "Hello");
				z.AddEntry("bar", "World");

				z.Save(bs);
			}

			bs.Seek(0, SeekOrigin.Begin);

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Zip'/>
		</Blob>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var block = dom.dataModels[0][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);


		}

		[Test]
		public void Crack2()
		{
			var bs = new BitStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("foo.xml", "<Elem>Hello</Elem>");
				z.AddEntry("bar.bin", "World");

				z.Save(bs);
			}

			bs.Seek(0, SeekOrigin.Begin);

			string xml = @"
<Peach>
	<DataModel name='XmlModel'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<DataModel name='BinModel'>
		<Blob>
			<Analyzer class='Binary'/>
		</Blob>
	</DataModel>

	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Zip'>
				<Param name='Map' value='/\.xml$/XmlModel/,/\.bin$/BinModel/' />
			</Analyzer>
		</Blob>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[2], bs);

			Assert.AreEqual(1, dom.dataModels[2].Count);
			var block = dom.dataModels[2][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);
			Assert.AreEqual(2, block.Count);
		}
	}
}

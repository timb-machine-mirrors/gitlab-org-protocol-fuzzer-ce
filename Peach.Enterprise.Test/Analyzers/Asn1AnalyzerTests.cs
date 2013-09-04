using System;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using Asn1Lib;
using System.Text;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

namespace Peach.Enterprise.Test.Analyzers
{
	[TestFixture]
	class Asn1AnalyzerTests
	{
		static MemoryStream googleDer = LoadResource("google.der");

		static string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Asn1'/>
		</Blob>
	</DataModel>
</Peach>";

		[SetUp]
		public void SetUp()
		{
			googleDer.Seek(0, SeekOrigin.Begin);
		}

		[Test]
		public void Test1()
		{
			var der = Asn1.Decode(googleDer);

			var ms = new MemoryStream();

			Asn1.ExportText(ms, der);

			var final = Encoding.ASCII.GetString(ms.ToArray());

			Console.WriteLine(final);

			Assert.NotNull(der);
		}

		private static MemoryStream LoadResource(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			var res = asm.GetName().Name + "." + name;
			using (var stream = asm.GetManifestResourceStream(res))
			{
				var ms = new MemoryStream();
				stream.CopyTo(ms);
				return ms;
			}
		}

		[Test]
		public void ParserTest()
		{
			var data = new MemoryStream(File.ReadAllBytes("C:\\Users\\seth\\Downloads\\TEST_SUITE\\TEST_SUITE\\tc38.ber"));

			var ber = Asn1.Decode(data);

			var ms = new MemoryStream();

			Asn1.ExportText(ms, ber);

			var final = Encoding.ASCII.GetString(ms.ToArray());

			Console.WriteLine(final);

		}

		[Test]
		public void Crack1()
		{
			var data = new MemoryStream(File.ReadAllBytes("C:\\Users\\seth\\Downloads\\TEST_SUITE\\TEST_SUITE\\tc38.ber"));
			CrackData(data);
		}

		private static void CrackData(Stream data)
		{

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			data.Seek(0, SeekOrigin.Begin);
			var bs = new BitStream(data);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

		}
	}
}

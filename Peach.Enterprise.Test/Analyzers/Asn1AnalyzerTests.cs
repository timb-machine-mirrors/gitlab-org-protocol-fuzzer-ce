using System;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using System.Text;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;
using Peach;

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
		public void TestGoogle()
		{
			CrackData(googleDer);
		}

		[Test]
		public void Crack1()
		{
			// Constructed Indefinite Length BitString comprised of 2 BitStrings
			// First has no unused bits
			// Second has unused bits
			var data = new byte[] { 0x23, 0x80, 0x03, 0x03, 0x00, 0x0a, 0x3b, 0x03, 0x05, 0x04, 0x5f, 0x29, 0x1c, 0xd0, 0x00, 0x00 };
			CrackData(new MemoryStream(data));
		}

		private static void CrackData(MemoryStream data)
		{

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			data.Seek(0, SeekOrigin.Begin);
			var bs = new BitStream(data);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			var final = dom.dataModels[0].Value.ToArray();
			var expected = data.ToArray();

			Assert.AreEqual(expected, final);

		}
	}
}

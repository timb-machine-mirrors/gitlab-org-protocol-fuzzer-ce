using System;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using System.Linq;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;
using Peach;
using Peach.Core;

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

		[Test]
		public void Crack2()
		{
			CrackData(new MemoryStream(TestCert));
		}

		[Test]
		public void Fuzz()
		{
			var tmp = Path.GetTempFileName();
			File.WriteAllBytes(tmp, TestCert);

			string pit = @"
<Peach>
	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Asn1'/>
		</Blob>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Strategy class='Sequential'/>
		<Publisher class='Null'/>
		<Mutators mode='include'>
			<Mutator class='NumericalEdgeCaseMutator'/>
		</Mutators>
	</Test>
</Peach>".Fmt(tmp);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(System.Text.Encoding.ASCII.GetBytes(pit)));

			var e = new Engine(null);
			var cfg = new RunConfiguration();

			e.startFuzzing(dom, cfg);

			Assert.AreEqual(20000, e.context.currentIteration);
		}

		private static void CrackData(MemoryStream data)
		{

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(System.Text.Encoding.ASCII.GetBytes(xml)));

			data.Seek(0, SeekOrigin.Begin);
			var bs = new BitStream(data);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			var final = dom.dataModels[0].Value.ToArray();
			var expected = data.ToArray();

			File.WriteAllBytes("C:\\Work\\badasn1.bin", final);

			Assert.AreEqual(expected, final);

		}

		byte[] TestCert = Peach.Core.HexString.Parse(@"
3082 03bf 3082 02a7 a003 0201 0202 0900
8d03 b939 393e ae35 300d 0609 2a86 4886
f70d 0101 0505 0030 7631 0b30 0906 0355
0406 1302 5553 3113 3011 0603 5504 080c
0a57 6173 6869 6e67 746f 6e31 1030 0e06
0355 0407 0c07 5365 6174 746c 6531 1430
1206 0355 040a 0c0b 5675 6c6e 2053 6572
7665 7231 1030 0e06 0355 040b 0c07 5353
4c20 4275 6731 1830 1606 0355 0403 0c0f
3139 322e 3136 382e 3232 322e 3134 3130
1e17 0d31 3430 3431 3031 3731 3132 315a
170d 3431 3038 3235 3137 3131 3231 5a30
7631 0b30 0906 0355 0406 1302 5553 3113
3011 0603 5504 080c 0a57 6173 6869 6e67
746f 6e31 1030 0e06 0355 0407 0c07 5365
6174 746c 6531 1430 1206 0355 040a 0c0b
5675 6c6e 2053 6572 7665 7231 1030 0e06
0355 040b 0c07 5353 4c20 4275 6731 1830
1606 0355 0403 0c0f 3139 322e 3136 382e
3232 322e 3134 3130 8201 2230 0d06 092a
8648 86f7 0d01 0101 0500 0382 010f 0030
8201 0a02 8201 0100 aed0 c396 6b30 c6c3
a032 2d3a bbaa 2442 8cfe 355a c3eb 38e5
723e 3e74 db51 e58e 2067 1183 cf91 9f7c
b177 8620 bb6f af48 df26 551e 979d fdaf
5c4a 2c14 3b8d eca2 9c7c dcc7 6903 5637
4e13 4e7a 7d1c 2599 5f82 85d0 003c 7b9a
7df4 24fd 6a46 2e9c fad0 7aaf 6df4 d080
81ed 49bf 5214 12b7 411f 0860 ac87 bb9f
1d18 7d09 2747 9375 1d6c 23da 2972 f564
99e4 8731 6b6b 2f85 d03d 0e6c e61d bc13
20d5 efd2 7a9a 3f3b 487b 5fc2 bac6 014a
597d 9f76 8993 4093 4ae7 37d4 c0cc 4b4f
1ef8 ff55 d6be 5901 e9d8 738b ed37 86b0
aacd 722d 55e8 2046 4a7a f8e8 c550 38db
0a66 e215 2917 0569 7de4 8016 806a c7ee
419b 5c3b 08e1 ca5c c01b 9d64 8c04 be9f
bb6f 968c a40f 22c1 0203 0100 01a3 5030
4e30 1d06 0355 1d0e 0416 0414 2891 565b
7f82 52b8 7fca 6b50 3dc0 e5d8 d97f 0477
301f 0603 551d 2304 1830 1680 1428 9156
5b7f 8252 b87f ca6b 503d c0e5 d8d9 7f04
7730 0c06 0355 1d13 0405 3003 0101 ff30
0d06 092a 8648 86f7 0d01 0105 0500 0382
0101 0019 2b07 09cb 32fd 1e5c d75b 3ef9
96bd 1af0 8bb1 5a18 3a66 2452 1aff debf
a2c5 8c88 8984 e7cd 8a84 4430 ee89 f248
cbc4 cf05 6a9e dc65 014c 78fc e339 5cc2
e489 317b e96c afcc e31a d7af 49c8 50e2
103c 19b2 03ba d43b c49b dccd 4cec 3e0a
8a59 018b cccc e758 495c c2a4 eaf3 ebe2
897c 69e0 266c 9456 169f ae1c 40af 42e8
1d70 9067 0e70 81f8 ce27 a7b7 0a95 39de
3bc1 245b 68cd 4616 4ede f0a8 e608 3704
eb77 4919 c395 2a8d 1763 04d8 fb10 725e
8633 5095 05d8 e7ca 4d49 ef94 4ab9 a4a5
2a7a 4090 cdd7 6840 6ba2 beb8 a64f 8ded
c2e4 6121 817c bdc1 9a2b 682b 9f81 b894
df79 d331 f67c 46f1 d148 5e36 97e5 b7d7
ccd6 a47c 2809 ba6c fbdd 8cfe 7958 9b8c
efcd d2                                ".Replace("\r\n", "")).Value;

		[Test]
		public void TestInternalValue()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='Type' size='8'/>
		<Block name='Value'>
			<Number name='SubType' size='8'/>
			<Number name='Length' size='24' endian='big'>
				<Relation type='size' of='Payload'/>
			</Number>
			<Block name='Payload'>
				<Number name='CertLength' size='24' endian='big'>
					<Relation type='size' of='CertPayload'/>
				</Number>
				<Block name='CertPayload'>
					<Blob name='Cert'>
						<Analyzer class='Asn1'/>
					</Blob>
				</Block>
			</Block>
		</Block>
	</DataModel>
</Peach>
";



			var input = Bits.Fmt("{0:b8}{1:b8}{2:b24}{3:b24}{4}", 8, 9, TestCert.Length + 3, TestCert.Length, TestCert);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], input);

			int cnt = dom.dataModels[0].Walk().Count();
			Assert.AreEqual(640, cnt);

			var iv = dom.dataModels[0][1].InternalValue;
			var buf = iv.BitsToArray();

			Assert.AreEqual(TestCert.Length + 7, buf.Length);

		}

		[Test]
		public void TestBadAsn1()
		{
			// Should throw a peach exception
			var ms = new MemoryStream();
			ms.Write(new byte[] { 0x00, 0x03, 0xc3 }, 0, 3);
			ms.Write(TestCert, 0, TestCert.Length);
			ms.Seek(0, SeekOrigin.Begin);

			try
			{
				CrackData(ms);
				Assert.Fail("should throw");
			}
			catch (CrackingFailure ex)
			{
				Assert.True(ex.Message.Contains("Asn1 analyzer only consumed 5 of 966 total bytes."));
			}
		}
	}
}

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Transformers
{
	[TestFixture]
	[Quick]
	[Peach]
	class TruncateTests : DataModelCollector
	{
		[Test]
		public void BlobTest()
		{
			// standard test
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name=""Data"" value=""Hello"">
			<Transformer class=""Truncate"">
				<Param name=""Length"" value=""3"" />
			</Transformer>
		</Blob>
	</DataModel>
	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""DM""/>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Random"" />
	</Test>
</Peach>
";

			PitParser parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated result from Peach2.3 on the blob: "Hel"
			byte[] precalcResult = Encoding.ASCII.GetBytes("Hel");
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void OffsetTest()
		{
			// standard test
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name=""Data"" value=""Hello"">
			<Transformer class=""Truncate"">
				<Param name=""Length"" value=""3"" />
				<Param name=""Offset"" value=""1"" />
			</Transformer>
		</String>
	</DataModel>
	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""DM""/>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Random"" />
	</Test>
</Peach>
";

			PitParser parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated result from Peach2.3 on the blob: "Hel"
			byte[] precalcResult = Encoding.ASCII.GetBytes("ell");
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void LargeLengthTest()
		{
			// standard test
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name=""Data"" value=""Hello"">
			<Transformer class=""Truncate"">
				<Param name=""Length"" value=""10"" />
				<Param name=""Offset"" value=""2"" />
			</Transformer>
		</String>
	</DataModel>
	<StateModel name=""TheState"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""DM""/>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<StateModel ref=""TheState""/>
		<Publisher class=""Null""/>
		<Strategy class=""Random"" />
	</Test>
</Peach>
";

			PitParser parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated result from Peach2.3 on the blob: "Hel"
			byte[] precalcResult = Encoding.ASCII.GetBytes("llo");
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test, ExpectedException(typeof(PeachException))]
		public void MissingParam()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Transformer class='Truncate'/>
		</String>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (byte)'0');

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test, ExpectedException(typeof(System.NotImplementedException), ExpectedMessage = "The method or operation is not implemented.")]
		public void CrackTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Transformer class='Truncate'>
				<Param name=""Length"" value=""1"" />
			</Transformer>
		</String>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (byte)'0');

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test, ExpectedException(typeof(SoftException), ExpectedMessage = "Hex decode failed, invalid length.")]
		public void CrackBadLengthTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Transformer class='Hex'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (byte)'0');

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}
	}
}

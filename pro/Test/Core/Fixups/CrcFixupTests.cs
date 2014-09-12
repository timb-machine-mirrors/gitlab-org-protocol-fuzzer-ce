using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Core.Test.Fixups
{
	[TestFixture]
	class CrcFixupTests : DataModelCollector
	{
		[Test]
		public void TestDefault()
		{
			// standard test

			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <Number name=""CRC"" size=""32"" signed=""false"">
				           <Fixup class=""CrcFixup"">
				               <Param name=""ref"" value=""Data""/>
				           </Fixup>
				       </Number>
				       <Blob name=""Data"" value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheState"" initialState=""Initial"">
				       <State name=""Initial"">
				           <Action type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheState""/>
				       <Publisher class=""Null""/>
				   </Test>
				</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated checksum from Peach2.3 on the blob: "Hello"
			byte[] precalcChecksum = new byte[] { 0x82, 0x89, 0xD1, 0xF7 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void TestLegacy()
		{
			// standard test

			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <Number name=""CRC"" size=""32"" signed=""false"">
				           <Fixup class=""Crc32Fixup"">
				               <Param name=""ref"" value=""Data""/>
				           </Fixup>
				       </Number>
				       <Blob name=""Data"" value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheState"" initialState=""Initial"">
				       <State name=""Initial"">
				           <Action type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheState""/>
				       <Publisher class=""Null""/>
				   </Test>
				</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated checksum from Peach2.3 on the blob: "Hello"
			byte[] precalcChecksum = new byte[] { 0x82, 0x89, 0xD1, 0xF7 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void Test32Default()
		{
			// standard test

			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <Number name=""CRC"" size=""32"" signed=""false"">
				           <Fixup class=""CrcFixup"">
				               <Param name=""ref"" value=""Data""/>
							   <Param name=""type"" value=""CRC32""/>
				           </Fixup>
				       </Number>
				       <Blob name=""Data"" value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheState"" initialState=""Initial"">
				       <State name=""Initial"">
				           <Action type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheState""/>
				       <Publisher class=""Null""/>
				   </Test>
				</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated checksum from Peach2.3 on the blob: "Hello"
			byte[] precalcChecksum = new byte[] { 0x82, 0x89, 0xD1, 0xF7 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}


		[Test]
		public void Test16Default()
		{
			// standard test

			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <Number name=""CRC"" size=""16"" signed=""false"">
				           <Fixup class=""CrcFixup"">
				               <Param name=""ref"" value=""Data""/>
							   <Param name=""type"" value=""CRC16""/>
				           </Fixup>
				       </Number>
				       <Blob name=""Data"" value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheState"" initialState=""Initial"">
				       <State name=""Initial"">
				           <Action type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheState""/>
				       <Publisher class=""Null""/>
				   </Test>
				</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			byte[] precalcChecksum = new byte[] { 0x53, 0xF3 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void TestCCITTDefault()
		{
			// standard test

			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <Number name=""CRC"" size=""16"" signed=""false"">
				           <Fixup class=""CrcFixup"">
				               <Param name=""ref"" value=""Data""/>
							   <Param name=""type"" value=""CRC_CCITT""/>
				           </Fixup>
				       </Number>
				       <Blob name=""Data"" value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheState"" initialState=""Initial"">
				       <State name=""Initial"">
				           <Action type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheState""/>
				       <Publisher class=""Null""/>
				   </Test>
				</Peach>";

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			byte[] precalcChecksum = new byte[] { 0xDA, 0xDA };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void CrackTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='CRC' signed='false' endian='big' size='16'>
			<Fixup class='Crc'>
				<Param name='ref' value='DM'/>
				<Param name='type' value='CRC16'/>
			</Fixup>
		</Number>
		<Blob name='Value' valueType='hex' value='ffff'/>
	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var initial = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(new byte[] { 0xb0, 0x01, 0xff, 0xff }, initial);

			var cracker = new Peach.Core.Cracker.DataCracker();
			cracker.CrackData(dom.dataModels[0], new BitStream(initial));

			var final = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(initial, final);
		}

	}
}

// end

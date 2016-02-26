using System.Data.SQLite;
using System;
using System.IO;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Peach]
	[Quick]
	class SampleNinjaMutatorTest : DataModelCollector
	{

		// 1. Create Ninja DB and verify
		// 2. Run some mutations and verify no exceptions

		byte [] ninjaSample1 = new byte[]{
			0x58, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A 
		};

		byte [] ninjaSample2 = new byte[]{
			0x57, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A 
		};

		byte[] ninjaSample3 = new byte[]{
			0x56, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A 
		};

		string ninjaSampleXml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='NinjaDm'>
	
		<Block name='Block' maxOccurs='-1'>
			<Number name='Num16' size='16' value='600' />
			<String name='Str' value='Peachy' token='true' />
			<Blob name='Blob' value='Blobby' token='true' />
			
			<Flags name='Flgs' size='8'>
				<Flag name='F1' size='1' position='1' />
				<Flag name='F2' size='1' position='2' />
			</Flags>
		</Block>
		
		<Asn1Type class='2' pc='1' tag='0' name='terminationID'>
			<String name='Value' value='Peachy'/>
		</Asn1Type>
		
		<Choice name='Choice'>
			<Number name='Num6' size='16' value='600' token='true' />
			<Number name='Num7' size='16' value='700' token='true' />
		</Choice>

		<String name='JsonTest'>
			<Analyzer class='Json'/>
		</String>
		
		<String value='\n' token='true' />
		
		<Padding />

	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action type='output'>
			<DataModel ref='NinjaDm'/>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='1024'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='SampleNinja' />
		</Mutators>
	</Test>
</Peach>
";

		// Use this model to generate bin file.
//		string ninjaSampleXmlGenerateBin = @"<?xml version='1.0' encoding='utf-8'?>
//<Peach>
//	<!-- Used to generate bin file -->
//	<DataModel name='NinjaDmOut'>
//	
//		<Block name='Block' maxOccurs='-1'>
//			<Number name='Num16' size='16' value='600' />
//			<String name='Str' value='Peachy' token='true' />
//			<Blob name='Blob' value='Blobby' token='true' />
//			
//			<Flags name='Flgs' size='8'>
//				<Flag name='F1' size='1' position='1' />
//				<Flag name='F2' size='1' position='2' />
//			</Flags>
//		</Block>
//		
//		<Asn1Type class='2' pc='1' tag='0' name='terminationID'>
//			<String name='Value' value='Peachy'/>
//		</Asn1Type>
//		
//		<Choice name='Choice'>
//			<Number name='Num6' size='16' value='600' token='true' />
//			<Number name='Num7' size='16' value='700' token='true' />
//		</Choice>
//
//		<Json name='JsonTest'>
//			<Number name='Num16' size='16' value='600' />
//			<String name='Str' value='Peachy' />
//			<Blob name='Blob' value='Blobby' />
//			<Null name='null'/>
//			<Bool name='bool' value='1'/>
//		</Json>
//		
//		<String value='\n' token='true' />
//		
//		<Padding />
//
//	</DataModel>
//
//	<StateModel name='TheStateModel' initialState='initial'>
//		<State name='initial'>
//		  <Action type='output'>
//			<DataModel ref='NinjaDm'/>
//   			<Data>
//				<Field name='Block[0]' value='' />
//				<Field name='Block[1]' value='' />
//				<Field name='Block[2]' value='' />
//			</Data>
//		  </Action>
//		</State>
//	</StateModel>
//
//	<Test name='Default' maxOutputSize='200'>
//		<StateModel ref='TheStateModel'/>
//		<Publisher class='File'>
//			<Param name='FileName' value='ninja.bin' />
//		</Publisher>
//	</Test>
//</Peach>
//";
		string tmpPath;
		string pitFile;
		string pitSamplePath;
		string pitSample1File;
		string pitSample2File;
		string pitSample3File;
		string ninjaDbFile;

		[SetUp]
		public void BuildNinjaDatabase()
		{
			tmpPath = Path.GetTempFileName();
			pitFile = Path.Combine(tmpPath, "ninja.xml");
			pitSamplePath = Path.Combine(tmpPath, "samples");
			pitSample1File = Path.Combine(pitSamplePath, "ninja1.bin");
			pitSample2File = Path.Combine(pitSamplePath, "ninja2.bin");
			pitSample3File = Path.Combine(pitSamplePath, "ninja3.bin");
			ninjaDbFile = pitFile + ".ninja";

			try
			{
				File.Delete(tmpPath);
				Directory.CreateDirectory(tmpPath);
				Directory.CreateDirectory(pitSamplePath);

				File.WriteAllText(pitFile, ninjaSampleXml);
				File.WriteAllBytes(pitSample1File, ninjaSample1);
				File.WriteAllBytes(pitSample2File, ninjaSample2);
				File.WriteAllBytes(pitSample3File, ninjaSample3);

				PeachSampleNinja.Program.Main(new[] { pitFile, "NinjaDm", pitSamplePath });

				Assert.IsTrue(File.Exists(ninjaDbFile));
			}
			catch
			{
				if (Directory.Exists(tmpPath))
					Directory.Delete(tmpPath, true);
				if (File.Exists(tmpPath))
					File.Delete(tmpPath);

				throw;
			}
		}

		[TearDown]
		public void DeleteNinjaDatabase()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();

			if (Directory.Exists(tmpPath))
				Directory.Delete(tmpPath, true);
			if (File.Exists(tmpPath))
				File.Delete(tmpPath);
		}

		[Test]
		public void VerifyNinjaDatabase()
		{
			using (var Connection = new SQLiteConnection("data source=" + ninjaDbFile))
			{
				Connection.Open();

				Guid definitionId;

				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = @"select definitionid, name from definition";

					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						definitionId = reader.GetGuid(0);
						Assert.AreEqual("ninja.xml", reader.GetString(1));
						Assert.False(reader.Read());
					}
				}

				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = @"select count('x') from element";

					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.AreEqual(21, reader.GetInt32(0));
					}
				}

				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = @"select sampleid, file from sample where definitionid = ?";
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters[0].Value = definitionId;

					using (var reader = cmd.ExecuteReader())
					{
						var sampleCount = 0;
						while (reader.Read())
						{
							sampleCount++;

							var sampleId = reader.GetGuid(0);
							Assert.AreEqual("ninja" + sampleCount + ".bin", reader.GetString(1));

							using (var cmd2 = new SQLiteCommand(Connection))
							{
								cmd2.CommandText = @"select count('x') from sampleelement where sampleid = ?";
								cmd2.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
								cmd2.Parameters[0].Value = sampleId;

								using (var reader2 = cmd2.ExecuteReader())
								{
									Assert.IsTrue(reader2.Read());
									Assert.AreEqual(39, reader2.GetInt32(0));
								}
							}
						}

						Assert.AreEqual(3, sampleCount);
					}
				}
			}
		}

		[Test]
		public void TestMutations()
		{
			RunEngine(ninjaSampleXml, pitFile);
			Assert.AreEqual(123, mutatedDataModels.Count);
		}
	}
}

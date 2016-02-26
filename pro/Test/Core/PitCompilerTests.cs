using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Core;
using System;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class PitCompilerTests
	{
		TempDirectory root;

		[SetUp]
		public void Setup()
		{
			root = new TempDirectory();
		}

		[TearDown]
		public void Teardown()
		{
			root.Dispose();
		}

		[Test]
		public void TestElements()
		{
			var xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' />
		<Choice name='Choice'>
			<Block name='A'>
				<Choice name='Choice'>
					<Blob name='AA' />
					<Blob name='AB' />
				</Choice>
			</Block>
			<Block name='B'>
				<Choice name='Choice'>
					<Blob name='BA' />
					<Blob name='BB' />
					<Asn1Type name='ASN' tag='0'>
						<Block name='V' />
					</Asn1Type>
				</Choice>
			</Block>
		</Choice>
		<Block name='Array' occurs='10'>
			<Blob name='Item' />
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />

			<Action name='Open' type='open' publisher='tcp' />

			<Action name='Output' type='output'>
				<DataModel ref='DM'/>
				<Data fileName='##SamplePath##/##Seed##' />
			</Action>

			<Action type='input'>
				<DataModel ref='DM'/>
			</Action>

			<Action name='MessageId' type='slurp' valueXpath='//Request//messageId/Value' setXpath='//messageId/Value' />

			<Action type='message' status='foo' error='bar' />

			<Action name='Close' type='close' publisher='tcp' />

			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent' />
		</State>

		<State name='Blank'>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='SM' />
		<Publisher class='TcpListener'>
			<Param name='Interface' value='0.0.0.0' />
			<Param name='Port' value='##ListenPort##' />
		</Publisher>
	</Test>
</Peach>
";
			var config = @"
<PitDefines>
	<All>
		<String key='SamplePath' name='SamplePath' value='##PitLibraryPath##/_Common/Samples/Image'/>
		<String key='Seed' name='Seed' value='*.PNG'/>
		<String key='ListenPort' name='ListenPort' value='31337'/>
	</All>
</PitDefines>
";
			var filename = Path.Combine(root.Path, "TestElements.xml");
			File.WriteAllText(filename, xml);
			File.WriteAllText(filename + ".config", config);
			var samplesDir = Path.Combine(root.Path, "_Common", "Samples", "Image");
			Directory.CreateDirectory(samplesDir);
			File.WriteAllText(Path.Combine(samplesDir, "foo.PNG"), "nothing here");

			var compiler = new PitCompiler(root.Path, filename);
			compiler.Parse(false, false);
			var tree = compiler.MakeFields();
			var actual = JsonConvert.SerializeObject(tree);
			var expected = JsonConvert.SerializeObject(new[] {
				new PitField { Id = "Initial", Fields = {
					new PitField { Id = "Output", Fields = {
						new PitField { Id = "DM", Fields = {
							new PitField { Id = "DataElement_0" },
							new PitField { Id = "Choice", Fields = {
								new PitField { Id = "A", Fields = {
									new PitField { Id = "Choice", Fields = {
										new PitField { Id = "AA" },
										new PitField { Id = "AB" }
									}},
								}},
								new PitField { Id = "B", Fields = {
									new PitField { Id = "Choice", Fields = {
										new PitField { Id = "BA" },
										new PitField { Id = "BB" },
										new PitField { Id = "ASN", Fields =
										{
											new PitField { Id = "V" }
										}},
									}},
								}},
							}},
							new PitField { Id = "Array", Fields = {
								new PitField { Id = "Array", Fields = {
									new PitField { Id = "Item" }
								}}
							}}
						}}
					}}
				}}
			});
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestFields()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='Str1' />
		<Block name='Blk' fieldId='B'>
			<String name='Str2' />
		</Block>
		<Blob name='Blob' fieldId='C' />
	</DataModel>

	<DataModel name='DM3'>
		<Blob name='Blob' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' fieldId='state'>
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />

			<Action name='Open' type='open' publisher='tcp' />

			<Action type='output' fieldId='action1'>
				<DataModel ref='DM1' />
			</Action>

			<Action type='input'>
				<DataModel ref='DM1' />
			</Action>

			<Action name='ActionWithNoFieldId' type='output'>
				<DataModel ref='DM1' />
			</Action>

			<Action type='call' fieldId='action2' method='foo'>
				<Param>
					<DataModel name='DM2' fieldId='c'>
						<Stream streamName='foo' fieldId='d' />

						<Json fieldId='e'>
							<Double size='64' fieldId='f' />
							<Sequence fieldId='g'>
								<Null fieldId='h' />
								<Bool fieldId='i' />
							</Sequence>
						</Json>

						<Frag fieldId='j'>
							<Block name='Template' fieldId='k' />
							<Block name='Payload' fieldId='l' />
						</Frag>

						<Blob fieldId='m' />
						<Choice name='Choice' fieldId='n'>
							<Block name='A' fieldId='n1'>
								<Choice name='Choice' fieldId='n1c'>
									<Block name='A' fieldId='n1c1' />
									<Block name='B' fieldId='n1c2' />
								</Choice>
							</Block>
							<Block name='B' fieldId='n2'>
								<Choice name='Choice' fieldId='n2c'>
									<Block name='A' fieldId='n2c1' />
									<Block name='B' fieldId='n2c2' />
								</Choice>
							</Block>
						</Choice>
						<Number size='32' fieldId='o' />
						<Padding alignment='32' fieldId='p' />
						<Block minOccurs='0' fieldId='q'>
							<String fieldId='qq' />
						</Block>

						<Flags fieldId='r' size='32'>
							<Flag size='1' position='0' fieldId='s' />
						</Flags>

						<XmlElement fieldId='t' elementName='foo'>
							<XmlAttribute fieldId='u' attributeName='bar' />
						</XmlElement>
						<XmlElement fieldId='t' elementName='foo'>
							<XmlAttribute fieldId='t2' attributeName='bar' />
						</XmlElement>

						<Asn1Type tag='1' fieldId='v' />
						<Asn1Tag fieldId='w' />
						<Asn1Length fieldId='x' />
						<BACnetTag fieldId='y' />
						<VarNumber fieldId='z' />

						<Block>
							<Block name='Template' fieldId='kk' />
							<Block name='Payload' fieldId='ll' />
						</Block>

					</DataModel>
				</Param>
			</Action>

			<Action name='ActionWithDMWithNoFieldIds' type='output'>
				<DataModel ref='DM3'/>
			</Action>

			<Action name='MessageId' type='slurp' valueXpath='//Request//messageId/Value' setXpath='//messageId/Value' />

			<Action type='message' status='foo' error='bar' />

			<Action name='Close' type='close' publisher='tcp' />

			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent' />
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var filename = Path.Combine(root.Path, "TestFields.xml");
			File.WriteAllText(filename, xml);

			var compiler = new PitCompiler(root.Path, filename);
			compiler.Parse(false, false);
			var tree = compiler.MakeFields();
			var actual = JsonConvert.SerializeObject(tree);
			var expected = JsonConvert.SerializeObject(new[] {
				new PitField { Id = "state", Fields = {
					new PitField { Id = "action1", Fields = {
						new PitField { Id = "B" },
						new PitField { Id = "C" },
					}},
					new PitField { Id = "ActionWithNoFieldId", Fields = {
						new PitField { Id = "B" },
						new PitField { Id = "C" },
					}},
					new PitField { Id = "action2", Fields = {
						new PitField { Id = "c", Fields = {
							new PitField { Id = "d" },
							new PitField { Id = "e", Fields = {
								new PitField { Id = "f" },
								new PitField { Id = "g", Fields = {
									new PitField { Id = "h" },
									new PitField { Id = "i" },
								}},
							}},
							new PitField { Id = "j", Fields = {
								new PitField { Id = "k" },
								new PitField { Id = "l" },
							}},
							new PitField { Id = "m" },
							new PitField { Id = "n", Fields = {
								new PitField { Id = "n1", Fields = {
									new PitField { Id = "n1c", Fields = {
										new PitField { Id = "n1c1" },
										new PitField { Id = "n1c2" },
									}}
								}},
								new PitField { Id = "n2", Fields = {
									new PitField { Id = "n2c", Fields = {
										new PitField { Id = "n2c1" },
										new PitField { Id = "n2c2" },
									}}
								}},
							}},
							new PitField { Id = "o" },
							new PitField { Id = "p" },
							new PitField { Id = "q", Fields = {
								new PitField { Id = "qq" },
							}},
							new PitField { Id = "r", Fields = {
								new PitField { Id = "s" },
							}},
							new PitField { Id = "t", Fields = {
								new PitField { Id = "u" },
								new PitField { Id = "t2" },
							}},
							new PitField { Id = "v" },
							new PitField { Id = "w" },
							new PitField { Id = "x" },
							new PitField { Id = "y" },
							new PitField { Id = "z" },
							new PitField { Id = "kk" },
							new PitField { Id = "ll" },
						}},
					}},
					new PitField { Id = "ActionWithDMWithNoFieldIds", Fields = {
						new PitField { Id = "DM3", Fields = {
							new PitField { Id = "Blob" }
						}},
					}},
				}},
			});
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestNoConfig()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
		</State>
	</StateModel>
	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var filename = Path.Combine(root.Path, "TestNoConfig.xml");
			File.WriteAllText(filename, xml);

			var compiler = new PitCompiler(root.Path, filename);
			var errors = compiler.Run(true, false);

			CollectionAssert.IsEmpty(errors);
		}

		[Test]
		public void TestConfigNoErrors()
		{
			const string xml = @"
<Peach>
	<!-- ##SamplePath##/##Executable## -->
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
		</State>
	</StateModel>
	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			const string xmlConfig = @"
<PitDefines>
	<Group name='Group' description='xxx'>
		<Define name='SamplePath' key='SamplePath' value='foo' description='xxx' />
		<Define name='Executable' key='Executable' value='foo' description='yyy' />
	</Group>
</PitDefines>
";

			var xmlPath = Path.Combine(root.Path, "TestConfigNoErrors.xml");
			File.WriteAllText(xmlPath, xml);

			var xmlConfigPath = Path.Combine(root.Path, "TestConfigNoErrors.xml.config");
			File.WriteAllText(xmlConfigPath, xmlConfig);

			var compiler = new PitCompiler(root.Path, xmlPath);
			var actual = compiler.Run(true, false);

			actual.ForEach(x => Console.WriteLine(x));
			CollectionAssert.IsEmpty(actual);
		}

		[Test]
		public void TestConfigErrors()
		{
			const string xml = @"
<Peach>
	<!-- ##Executable## -->
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
		</State>
	</StateModel>
	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			const string xmlConfig = @"
<PitDefines>
	<Group name='Group'>
		<Define name='SamplePath' key='SamplePath' value='foo' description='xxx' />
		<Define name='Executable' key='Executable' value='foo' />
	</Group>
	<All>
	</All>
	<Windows>
	</Windows>
</PitDefines>
";

			var xmlPath = Path.Combine(root.Path, "TestConfigErrors.xml");
			File.WriteAllText(xmlPath, xml);

			var xmlConfigPath = Path.Combine(root.Path, "TestConfigErrors.xml.config");
			File.WriteAllText(xmlConfigPath, xmlConfig);

			var compiler = new PitCompiler(root.Path, xmlPath);
			var actual = compiler.Run(true, false);

			actual.ForEach(x => Console.WriteLine(x));

			var expected = new string[] {
				"Detected unused PitDefine: 'SamplePath'.",
				"PitDefine 'Group' missing 'Description' attribute.",
				"PitDefine 'Executable' missing 'Description' attribute.",
				"Configuration file should not have platform specific defines.",
			};

			CollectionAssert.AreEquivalent(expected, actual);
		}

		[Test]
		public void TestPitLintIgnore()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<!--
PEACH PIT COPYRIGHT NOTICE AND LEGAL DISCLAIMER
-->
<Peach 
	xmlns='http://peachfuzzer.com/2012/Peach'
	xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
	author='Peach Fuzzer, LLC'
	description='PIT'>

	<DataModel name='DM'>
		<String name='str1' value='0' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<!-- before -->
			<!-- PitLint: Skip_StartIterationEvent -->
			<!-- after -->
			<Action type='call' method='InitializeIterationEvent' publisher='Peach.Agent' />
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />
			<!-- PitLint: Allow_WhenControlIteration -->
			<Action name='Act1' type='output' when='context.controlIteration'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str1' value='Hello'/>
				</Data>
			</Action>
			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<!-- PitLint: Skip_Lifetime -->
	<Test name='Default' maxOutputSize='65535' targetLifetime='iteration'>
		<StateModel ref='TheState'/>
		<Publisher class='RawEther' name='pub1'>
			<Param name='Interface' value='##Interface##'/>
			<!-- Pit is send only, don't need to expose timeouts or filter -->
			<!-- PitLint: Allow_MissingParamValue=Timeout -->
			<!-- PitLint: Allow_MissingParamValue=Filter -->
		</Publisher>
		<Publisher class='RawEther' name='pub2'>
			<!-- Pit is send only, don't need to expose timeouts or filter -->
			<!-- PitLint: Allow_MissingParamValue=Timeout -->
			<!-- PitLint: Allow_MissingParamValue=Filter -->
			<Param name='Interface' value='##Interface##'/>
		</Publisher>
		<Publisher class='Null' name='null'>
			<!-- Comment -->
			<!-- PitLint: Allow_MissingParamValue=MaxOutputSize -->
		</Publisher>
	</Test>
</Peach>
";

			const string xmlConfig = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
</PitDefines>
";
			
			var xmlPath = Path.Combine(root.Path, "TestPitLintIgnore.xml");
			File.WriteAllText(xmlPath, xml);

			var xmlConfigPath = Path.Combine(root.Path, "TestPitLintIgnore.xml.config");
			File.WriteAllText(xmlConfigPath, xmlConfig);

			var compiler = new PitCompiler(root.Path, xmlPath);
			var actual = compiler.Run();

			actual.ForEach(x => Console.WriteLine(x));
			CollectionAssert.IsEmpty(actual);
		}

		[Test]
		public void TestNewlineInValue()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<!--
PEACH PIT COPYRIGHT NOTICE AND LEGAL DISCLAIMER
-->
<Peach 
	xmlns='http://peachfuzzer.com/2012/Peach'
	xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
	author='Peach Fuzzer, LLC'
	description='PIT'>

	<DataModel name='DM'>
		<String name='str1' value='new
line' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />
			<Action name='Act1' type='output'>
				<DataModel ref='DM'>
					<Blob name='blob' valueType='hex' value='00
11' />
					<String name='str2' value='another
new
line' />
				</DataModel>
				<Data>
					<Field name='str1' value='Bad
Field'/>
				</Data>
			</Action>
			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<!-- PitLint: Skip_Lifetime -->
	<Test name='Default' maxOutputSize='65535' targetLifetime='iteration'>
		<StateModel ref='TheState'/>
		<Publisher class='Null' name='null'>
			<!-- Comment -->
			<!-- PitLint: Allow_MissingParamValue=MaxOutputSize -->
		</Publisher>
	</Test>
</Peach>
";

			const string xmlConfig = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
</PitDefines>

";
			var xmlPath = Path.Combine(root.Path, "TestNewlineInValue.xml");
			File.WriteAllText(xmlPath, xml);

			var xmlConfigPath = Path.Combine(root.Path, "TestNewlineInValue.xml.config");
			File.WriteAllText(xmlConfigPath, xmlConfig);

			var compiler = new PitCompiler(root.Path, xmlPath);
			var actual = compiler.Run();

			var expected = new string[] {
				"Element has value attribute with embedded newline: <String name=\"str1\" value=\"new&#xD;&#xA;line\" xmlns=\"http://peachfuzzer.com/2012/Peach\" />",
				"Element has value attribute with embedded newline: <String name=\"str2\" value=\"another&#xD;&#xA;new&#xD;&#xA;line\" xmlns=\"http://peachfuzzer.com/2012/Peach\" />",
				"Element has value attribute with embedded newline: <Field name=\"str1\" value=\"Bad&#xD;&#xA;Field\" xmlns=\"http://peachfuzzer.com/2012/Peach\" />",
			};

			actual.ForEach(x => Console.WriteLine(x));
			CollectionAssert.AreEquivalent(expected, actual);
		}
	}
}

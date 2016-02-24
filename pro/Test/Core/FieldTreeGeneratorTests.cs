using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Core;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class FieldTreeGeneratorTests
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

			var tree = FieldTreeGenerator.MakeFields(root.Path, filename);
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

			var tree = FieldTreeGenerator.MakeFields(root.Path, filename);
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
	}
}

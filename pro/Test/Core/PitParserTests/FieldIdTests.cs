using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using Peach.Core.Dom.XPath;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	internal class FieldIdTests
	{
		[Test]
		public void TestPitParse()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='String' />
	</DataModel>

	<DataModel name='DM2'>
		<String name='Str1' />
		<Block name='Blk' fieldId='B'>
			<String name='Str2' />
		</Block>
		<Blob name='Blob' fieldId='C' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' fieldId='a'>
			<Action type='output' fieldId='z'>
				<DataModel name='DM' />
			</Action>

			<Action type='call' fieldId='b' method='foo'>
				<Param>
					<DataModel name='DM' fieldId='c'>
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
						<Choice fieldId='n' />
						<Number size='32' fieldId='o' />
						<Padding alignment='32' fieldId='p' />
						<String minOccurs='0' fieldId='q' />

						<Flags fieldId='r' size='32'>
							<Flag size='1' position='0' fieldId='s' />
						</Flags>

						<XmlElement fieldId='t' elementName='foo'>
							<XmlAttribute fieldId='u' attributeName='bar' />
						</XmlElement>

						<Asn1Type tag='1' fieldId='v' />
						<Asn1Tag fieldId='w' />
						<Asn1Length fieldId='x' />
						<BACnetTag fieldId='y' />
						<VarNumber fieldId='z' />
					</DataModel>
				</Param>
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			Assert.Null(dom.dataModels[0].FieldId);
			Assert.Null(dom.dataModels[0][0].FieldId);

			Assert.Null(dom.dataModels[1].FieldId);
			Assert.Null(dom.dataModels[1][0].FieldId);
			Assert.AreEqual("B", dom.dataModels[1][1].FieldId);
			Assert.Null(((Block)dom.dataModels[1][1])[0].FieldId);
			Assert.AreEqual("C", dom.dataModels[1][2].FieldId);

			var s = dom.stateModels[0].states[0];

			Assert.AreEqual("a", s.FieldId);
			Assert.AreEqual("z", s.actions[0].FieldId);
			Assert.Null(s.actions[0].outputData.First().dataModel.FieldId);

			var a = (Call)s.actions[1];
			Assert.AreEqual("b", a.FieldId);

			var fields = a.parameters[0].dataModel.PreOrderTraverse().Select(e => e.FieldId).ToList();

			var exp = new[]
			{
				"c",  // DataModel
				"d",  // Stream
				null, // Stream.Name
				null, // Stream.Attr
				null, // Stream.Content
				"e",  // Json
				"f",  // Double
				"g",  // Sequence
				"h",  // Null
				"i",  // Bool
				"j",  // Frag
				null, // Rendering
				"k",  // Template
				"l",  // Payload
				"m",  //Blob
				"n",  // Choice
				"o",  // Number
				"p",  // Padding
				null, // Array
				"q",  //String
				"r",  // Flags
				"s",  // Flag
				"t",  // XmlElement
				"u",  // XmlAttribute
				"v",  // Asn1Type
				null, // Asn1Type.class
				null, // Asn1Type.pc
				null, // Asn1Type.tag
				null, // Asn1Type.length
				"w",  // Asn1Tag
				"x",  //Asn1Length
				"y",  // BacNetTag
				null, // BacNetTag.Tag
				null, // BacNetTag.Class
				null, // BacNetTag.LenValueType
				"z"   // VarNumber
			};

			Assert.AreEqual(exp, fields);
		}

		[Test]
		public void TestXpath()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' />
		<State name='a' fieldId='a'>
			<Action type='output' name='b' fieldId='b'>
				<DataModel name='DM' />
			</Action>

			<Action type='output'>
				<DataModel name='c' fieldId='c' />
			</Action>

			<Action type='call' name='d' fieldId='d' method='foo'>
				<Param>
					<DataModel name='e' fieldId='e'>
						<Blob />
						<Block name='f' fieldId='f'>
							<String name='g' fieldId='g' />
						</Block>
					</DataModel>
				</Param>
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(dom.stateModels[0]);
			var iter = navi.Select("//*[@fieldId]", resolver);

			var result = new List<string>();

			while (iter.MoveNext())
			{
				var curr = (INamed)((PeachXPathNavigator)iter.Current).CurrentNode;

				result.Add(curr.Name);
			}

			CollectionAssert.IsNotEmpty(result);

			var exp = new[] { "a", "b", "c", "d", "e", "f", "g" };
			Assert.AreEqual(exp, result);
		}
	}
}

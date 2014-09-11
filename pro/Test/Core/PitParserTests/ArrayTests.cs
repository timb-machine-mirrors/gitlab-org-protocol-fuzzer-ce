﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
//using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Core.Test.PitParserTests
{
	[TestFixture]
	class ArrayTests
	{
		class Resetter : DataElement
		{
			public static void Reset()
			{
				DataElement._uniqueName = 0;
			}

			public override void Crack(Cracker.DataCracker context, IO.BitStream data, long? size)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void ArrayHintsTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"Hello World\" minOccurs=\"100\">" +
				"			<Hint name=\"Hello\" value=\"World\"/>"+
				"		</Blob>"+
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][0] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);

			Assert.NotNull(array.Hints);
			Assert.AreEqual(1, array.Hints.Count);
			Assert.AreEqual("World", array.Hints["Hello"].Value);
		}

		[Test]
		public void ArrayNameTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"stuff\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][0] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);
			Assert.AreEqual("TheDataModel.stuff", array.fullName);
			Assert.AreEqual("TheDataModel.stuff.stuff", array[0].fullName);
			Assert.AreEqual("TheDataModel.stuff.stuff_1", array[1].fullName);
			Assert.AreEqual(array, array[0].parent);
		}

		[Test]
		public void ArrayNoNameTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			Resetter.Reset();

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][0] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);
			Assert.AreEqual("TheDataModel.DataElement_0", array.fullName);
			Assert.AreEqual("TheDataModel.DataElement_0.DataElement_0", array[0].fullName);
			Assert.AreEqual("TheDataModel.DataElement_0.DataElement_0_1", array[1].fullName);
		}

		[Test]
		public void ArrayOfRelationTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"Length\" size=\"32\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][1] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);
			Assert.AreEqual(1, array.relations.Count);
			Assert.AreEqual(0, array[0].relations.Count);
		}

		[Test]
		public void ArrayFromRelationTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"Hello World\"/>" +
				"		<Number name=\"Length\" size=\"32\"  minOccurs=\"100\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][1] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);
			Assert.AreEqual(0, array.relations.Count);
			Assert.AreEqual(1, array[0].relations.Count);
		}

		[Test]
		public void TestArrayClone()
		{
			// If an array is cloned with a new name, the 1st element in the array needs
			// to have its name updated as well

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Dom.Array array = dom.dataModels[0][0] as Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(100, array.Count);
			Assert.AreEqual("Data", array.name);
			Assert.AreEqual("Data", array[0].name);

			var clone = array.Clone("NewData") as Dom.Array;

			Assert.NotNull(clone);
			Assert.AreEqual(100, clone.Count);
			Assert.AreEqual("NewData", clone.name);
			Assert.AreEqual("NewData", clone[0].name);
		}

		private void DoOccurs(string occurs, byte[] expected)
		{
			string template =
@"<Peach>
	<DataModel name=""DM"">
		<String value=""XYZ"" {0}/>
	</DataModel>
</Peach>";

			string xml = string.Format(template, occurs);

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var value = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(expected, value);
		}

		[Test]
		public void TestOccurs()
		{
			DoOccurs("minOccurs=\"5\"", Encoding.ASCII.GetBytes("XYZXYZXYZXYZXYZ"));
			DoOccurs("minOccurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("minOccurs=\"0\"", Encoding.ASCII.GetBytes(""));

			DoOccurs("occurs=\"5\"", Encoding.ASCII.GetBytes("XYZXYZXYZXYZXYZ"));
			DoOccurs("occurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("occurs=\"0\"", Encoding.ASCII.GetBytes(""));

			DoOccurs("maxOccurs=\"5\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("maxOccurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("maxOccurs=\"0\"", Encoding.ASCII.GetBytes("XYZ"));

		}

		[Test]
		public void TestArrayFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='1'>
			<String name='Value' value='***' />
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[0].Value' value='xxx'/>
					<Field name='Items[2].Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("xxx***zzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items",
				"DM.Items.Items.Value",
				"DM.Items.Items_1",
				"DM.Items.Items_1.Value",
				"DM.Items.Items_2",
				"DM.Items.Items_2.Value",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayArrayFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='1'>
			<Block name='SubItems' minOccurs='0'>
				<String name='Value' value='Value' />
			</Block>
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[1].SubItems[1].Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("Valuezzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items",
				"DM.Items.Items.SubItems",
				"DM.Items.Items.SubItems.SubItems",
				"DM.Items.Items.SubItems.SubItems.Value",
				"DM.Items.Items_1",
				"DM.Items.Items_1.SubItems",
				"DM.Items.Items_1.SubItems.SubItems",
				"DM.Items.Items_1.SubItems.SubItems.Value",
				"DM.Items.Items_1.SubItems.SubItems_1",
				"DM.Items.Items_1.SubItems.SubItems_1.Value",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayChoiceFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='Items' minOccurs='1'>
			<Block name='One'>
				<String name='Value' value='Value One' />
			</Block>
			<Block name='Two'>
				<String name='Value' value='Value One' />
			</Block>
			<Block name='Three'>
				<String name='Value' value='Value One' />
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[0].Two.Value' value='xxx'/>
					<Field name='Items[1].Three.Value' value='yyy'/>
					<Field name='Items[2].One.Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("xxxyyyzzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items",
				"DM.Items.Items.Two",
				"DM.Items.Items.Two.Value",
				"DM.Items.Items_1",
				"DM.Items.Items_1.Three",
				"DM.Items.Items_1.Three.Value",
				"DM.Items.Items_2",
				"DM.Items.Items_2.One",
				"DM.Items.Items_2.One.Value",
			};

			Assert.AreEqual(names, exp);
		}
	}
}

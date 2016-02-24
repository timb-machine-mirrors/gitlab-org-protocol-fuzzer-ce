using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Core.Dom;
using Peach.Pro.Core.Dom;
using String = Peach.Core.Dom.String;

namespace Peach.Pro.Test.Core.Analyzers
{
	[TestFixture]
	[Category("Peach")]
	[Quick]
	class JsonDepricatedAnalyzerTests : DataModelCollector
	{
		[Test]
		public void SimpleTest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value=""{}"">
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json)dom.dataModels["TheDataModel"][0];

			Assert.AreEqual("{}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void JsonSerialization()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""string"":""peachy"",""bool"":true,""null"":null,""array"":[1,2,3,4]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			Assert.DoesNotThrow(() =>
			{
				using (var sout = new MemoryStream())
				using (var writer = XmlWriter.Create(sout, settings))
				{
					dom.dataModels["TheDataModel"].WritePit(writer);
				}
			});
		}

		[Test]
		public void StringTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":""Bar""}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is String);

			Assert.AreEqual("{\"Foo\":\"Bar\"}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void StringTest2()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":""Bar"",""Foo2"":""Bar2""}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is String);
			Assert.IsTrue(elem1[1] is String);

			Assert.AreEqual("{\"Foo\":\"Bar\",\"Foo2\":\"Bar2\"}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NumberTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":1}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is Number);

			Assert.AreEqual("{\"Foo\":1}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NumberTest2()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":-1}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is Number);

			Assert.AreEqual("{\"Foo\":-1}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NumberTest3()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":18446744073709551615'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is Number);

			Assert.AreEqual("{\"Foo\":18446744073709551615}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NumberTest4()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":0'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is Number);

			Assert.AreEqual("{\"Foo\":0}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NumberTest5()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":-9223372036854775808'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];

			Assert.IsTrue(elem1[0] is Number);

			Assert.AreEqual("{\"Foo\":-9223372036854775808}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void SequenceTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":[""Bar"",""Baz""]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Sequence);

			var elem2 = (Sequence) elem1[0];
			Assert.AreEqual(2, elem2.Count);

			Assert.IsTrue(elem2[0] is String);

			Assert.AreEqual("{\"Foo\":[\"Bar\",\"Baz\"]}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void SequenceTest2()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":[""Bar"",1]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Sequence);

			var elem2 = (Sequence) elem1[0];
			Assert.AreEqual(2, elem2.Count);

			Assert.IsTrue(elem2[0] is String);
			Assert.IsTrue(elem2[1] is Number);

			Assert.AreEqual("{\"Foo\":[\"Bar\",1]}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void ObjectTest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":{""Bar"":""Baz""}}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Block);

			var elem2 = (Block) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);
			Assert.AreEqual(1, elem2.Count);

			Assert.IsTrue(elem2[0] is String);

			Assert.AreEqual("{\"Foo\":{\"Bar\":\"Baz\"}}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void ObjectSequenceTest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":{""Bar"":[""Foo"",""Baz""]}}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Block);

			var elem2 = (Block) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);
			Assert.AreEqual(1, elem2.Count);
			Assert.IsTrue(elem2[0] is Sequence);

			var elem3 = (Sequence) elem2[0];
			Assert.AreEqual("Bar", elem3.Name);
			Assert.AreEqual(2, elem3.Count);
			Assert.IsTrue(elem3[0] is String);

			Assert.AreEqual("{\"Foo\":{\"Bar\":[\"Foo\",\"Baz\"]}}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void SequenceObject()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":[{""Foo"":""Baz""}]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Sequence);

			var elem2 = (Sequence) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);
			Assert.AreEqual(1, elem2.Count);
			Assert.IsTrue(elem2[0] is Block);

			var elem3 = (Block) elem2[0];
			Assert.AreEqual(1, elem3.Count);
			Assert.IsTrue(elem3[0] is String);
			Assert.AreEqual("Foo", elem3[0].Name);

			Assert.AreEqual("{\"Foo\":[{\"Foo\":\"Baz\"}]}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NullValueTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":null}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Null);

			var elem2 = (Null) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);

			Assert.AreEqual("{\"Foo\":null}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NullInSequenceTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":[null]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Sequence);

			var elem2 = (Sequence) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);
			Assert.AreEqual(1, elem2.Count);
			Assert.IsTrue(elem2[0] is Null);

			Assert.AreEqual("{\"Foo\":[null]}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void BoolTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":true}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Bool);

			var elem2 = (Bool) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);

			Assert.AreEqual("{\"Foo\":true}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void BoolTest2()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":false}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json) dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Bool);

			var elem2 = (Bool) elem1[0];
			Assert.AreEqual("Foo", elem2.Name);

			Assert.AreEqual("{\"Foo\":false}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void BoolInSequenceTest1()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{""Foo"":[false]}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);
			var elem1 = (Json)dom.dataModels["TheDataModel"][0];
			Assert.IsTrue(elem1[0] is Sequence);

			var elem2 = (Sequence)elem1[0];
			Assert.AreEqual("Foo", elem2.Name);
			Assert.AreEqual(1, elem2.Count);
			Assert.IsTrue(elem2[0] is Bool);

			Assert.AreEqual("{\"Foo\":[false]}", elem1.InternalValue.BitsToString());

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void SequenceAsChildOfSequence()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
<Json>
      <Block name=""aaa"">
        <Sequence name=""bbb"">
          <Sequence>
            <Block>
              <Null name=""value"" />
            </Block>
          </Sequence>
		</Sequence>
      </Block>
    </Json>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Json);

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void NestedObjectsInArrayTest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value='{
  ""A"": {
    ""B"": [
      {
        ""C"": {
          ""D"": 150045,
          ""E"": {
            ""M"": 1,
            ""T"": 1
          }
        },
        ""F"": 0
      },
      {
        ""C"": {
          ""D"": 0,
          ""E"": {
            ""M"": 0,
            ""T"": 0
          }
        },
        ""F"": 0
      },
      {
        ""C"": {
          ""D"": 0,
          ""E"": {
            ""M"": 0,
            ""T"": 0
          }
        },
        ""F"": 0
      },
      {
        ""C"": {
          ""D"": 0,
          ""E"": {
            ""M"": 0,
            ""T"": 0
          }
        },
        ""F"": 0
      },
      {
        ""C"": {
          ""D"": 0,
          ""E"": {
            ""M"": 0,
            ""T"": 0
          }
        },
        ""F"": 0
      }
    ]
  }
}'>
			<Analyzer class=""JsonDepricated""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

	}
}

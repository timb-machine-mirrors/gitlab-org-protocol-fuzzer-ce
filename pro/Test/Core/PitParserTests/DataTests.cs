using System.Collections.Generic;
using System.IO;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class DataTests
	{
		[Test]
		public void DataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""'Hello, world!'"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, config);

			var elem = (String)dom.tests[0].stateModel.states[0].actions[0].dataModel[0];
			Assert.AreEqual("Hello, world!", (string)elem.DefaultValue);
		}

		[Test]
		public void NullDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""None"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Error, the value of the eval statement of Field 'String' returned null.", ex.Message);
		}

		[Test]
		public void BadTypeDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""{}"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Error, the value of the eval statement of Field 'String' returned unsupported type 'IronPython.Runtime.PythonDictionary'.", ex.Message);
		}

		[Test]
		public void InvalidDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""foo.bar()"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Failed to evaluate expression [foo.bar()], name 'foo' is not defined.", ex.Message);
		}
	}
}

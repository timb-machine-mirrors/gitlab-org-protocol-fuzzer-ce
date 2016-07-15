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
	class DataTests : DataModelCollector
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

			var dom = ParsePit(xml);

			var stream = new MemoryStream();
			dom.tests[0].publishers[0] = new StateModel.MemoryStreamPublisher(stream);

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			var elem = dom.tests[0].stateModel.states[0].actions[0].dataModel[0] as String;
			Assert.AreEqual("Hello, world!", (string)elem.DefaultValue);
		}
	}
}

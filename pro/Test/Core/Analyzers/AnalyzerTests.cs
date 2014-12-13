using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Analyzers
{
	public class AnalyzerTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void TestFieldData()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'>
			<Analyzer class='StringToken'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Some,String,To,Split'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, dataModels.Count);

			int numElements = dataModels[0].EnumerateAllElements().Count();

			Assert.AreEqual(11, numElements);
		}
	}

}
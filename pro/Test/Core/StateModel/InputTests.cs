using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.StateModel
{
	[TestFixture] [Category("Peach")]
	class InputTests
	{
		[Test]
		public void Test1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String value=\"Hello\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"input\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheStateModel\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomDeterministic\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			MemoryStream stream = new MemoryStream(ASCIIEncoding.ASCII.GetBytes("Hello World!"));
			dom.tests[0].publishers[0] = new MemoryStreamPublisher(stream);

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual("Input", state.actions.First().type);
			Assert.AreEqual(ASCIIEncoding.ASCII.GetBytes("Hello World!"), state.actions[0].dataModel.Value.ToArray());
		}
	}
}

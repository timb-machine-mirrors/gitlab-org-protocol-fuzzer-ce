using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;

namespace Peach.Pro.Test.Core.StateModel
{
	[TestFixture] [Category("Peach")]
	class SlurpTests
	{
		[Test]
		public void Test1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String name=\"String1\" value=\"1234567890\"/>" +
				"   </DataModel>" +
				"   <DataModel name=\"TheDataModel2\">" +
				"       <String name=\"String2\" value=\"Hello World!\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				
				"           <Action name=\"Action2\" type=\"slurp\" valueXpath=\"//String1\" setXpath=\"//String2\" />"+

				"           <Action name=\"Action3\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"           </Action>" +

				"           <Action name=\"Action4\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
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

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[2].dataModel.Value.ToArray());
			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[3].dataModel.Value.ToArray());
		}

		[Test]
		public void Test2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String name=\"String1\" value=\"1234567890\"/>" +
				"   </DataModel>" +
				"   <DataModel name=\"TheDataModel2\">" +
				"       <String name=\"String2\" value=\"Hello World!\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +

				"           <Action name=\"Action2\" type=\"slurp\" valueXpath=\"//Action1/TheDataModel1/String1\" setXpath=\"//String2\" />" +

				"           <Action name=\"Action3\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +

				"           <Action name=\"Action4\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
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

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[2].dataModel.Value.ToArray());
			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[3].dataModel.Value.ToArray());
		}

		[Test]
		public void SlurpSelectedFieldOutput()
		{
			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='strSource' value='Hello'/>
	</DataModel>

<DataModel name='DM2'>
		<Choice name='q'>
			<Blob name='blob'/>
			<Block name='blk'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action> 

			<Action type='slurp' valueXpath='//strSource' setXpath='//str1' />

			<Action type='output'>
				<DataModel ref='DM2'/>
				<Data>
					<Field name='q.blk.str2' value='World' />
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration
			{
				singleIteration = true,
			};

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var dm = dom.tests[0].stateModel.states["initial"].actions[2].dataModel;
			var val = dm.Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("HelloWorld");

			Assert.AreEqual(exp, val);
		}

		[Test]
		public void SlurpSelectedFieldOutputSwitch()
		{
			// Ensure that slurp will set values for non-selected choice elements
			// The engine runs, and before the state model starts, all data models
			// get cached in originalDataModel.  When this happens, DM2 uses the
			// first data set called "Field1" that picks the choice "blk1"

			// The state model starts to run, the slurp action runs which should
			// set every element called "str1" to have the value "Hello".

			// The final output action runs, and the RandomStrategy switches the
			// dataSet to "Field2".  This dataSet selects choice "blk2"

			// The user expects the "str1" in "blk2" to be "Hello" because of the slurp.

			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='strSource' value='Hello'/>
	</DataModel>

	<DataModel name='DM2'>
		<Choice name='q'>
			<Blob name='blob'/>
			<Block name='blk1'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
			<Block name='blk2'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action> 

			<Action type='slurp' valueXpath='//strSource' setXpath='//str1' />

			<Action type='output'>
				<DataModel ref='DM2'/>
				<Data name='Field1'>
					<Field name='q.blk1.str2' value='World 1' />
				</Data>
				<Data name='Field2'>
					<Field name='q.blk2.str2' value='World 2' />
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='2' />
		</Strategy>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration
			{
				singleIteration = true,
				randomSeed = 1,
				skipToIteration = 9,
			};

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var actionData = dom.tests[0].stateModel.states["initial"].actions[2].allData.First();
			Assert.AreEqual("Field2", actionData.selectedData.name);
			var dm = actionData.dataModel;
			var val = Encoding.ASCII.GetString(dm.Value.ToArray());

			Assert.AreEqual("HelloWorld 2", val);
		}

	}
}

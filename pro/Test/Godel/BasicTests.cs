using System;
using System.IO;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

using NUnit.Framework;
//using NUnit.Framework.Constraints;

using Peach.Core;
using System.Collections.Generic;
//using Peach.Core.Dom;
//using Peach.Core.Analyzers;
//using Peach.Core.IO;

namespace Godel.Tests
{
	[TestFixture]
	class BasicTests
	{
		[Test]
		public void ParserTest()
		{
			string xml = @"
<Peach>

	<DataModel name='DM'>
		<String value='Hello World'/>
	</DataModel>

	<Godel name='BasicContext' inv='1 == 1' pre='2 == 2' post='3 == 3'/>

	<Godel name='DerivedContext' ref='BasicContext' post='4 == 4'/>

	<Godel name='ControlOnly' ref='DerivedContext' controlOnly='true'/>

	<StateModel name='SM' initialState='Initial'>
		<Godel ref='DerivedContext' pre='5 == 5'/>

		<State name='Initial'>
			<Godel ref='BasicContext'/>

			<Action type='output'>
				<Godel ref='ControlOnly'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel inv='True == True'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel ref='DerivedContext' post='6 == 6'/>
				<DataModel ref='DM'/>
			</Action>

			<Action type='output'>
				<Godel ref='ControlOnly' controlOnly='false'/>
				<DataModel ref='DM'/>
			</Action>
		</State>

	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>

</Peach>
";
			var parser = new Godel.Core.GodelPitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;

			Assert.NotNull(dom);

			Assert.AreEqual(3, dom.godel.Count);

			var sm = dom.stateModels[0] as Godel.Core.StateModel;

			Assert.NotNull(sm);

			Assert.AreEqual(6, sm.godel.Count);

			Assert.AreEqual(sm.godel[0].name, "SM.Initial.Action");
			Assert.AreEqual(sm.godel[0].controlOnly, true);
			Assert.AreEqual(sm.godel[0].inv, "1 == 1");
			Assert.AreEqual(sm.godel[0].pre, "2 == 2");
			Assert.AreEqual(sm.godel[0].post, "4 == 4");

			Assert.AreEqual(sm.godel[1].name, "SM.Initial.Action_1");
			Assert.AreEqual(sm.godel[1].controlOnly, false);
			Assert.AreEqual(sm.godel[1].inv, "True == True");
			Assert.AreEqual(sm.godel[1].pre, null);
			Assert.AreEqual(sm.godel[1].post, null);

			Assert.AreEqual(sm.godel[2].name, "SM.Initial.Action_2");
			Assert.AreEqual(sm.godel[2].controlOnly, false);
			Assert.AreEqual(sm.godel[2].inv, "1 == 1");
			Assert.AreEqual(sm.godel[2].pre, "2 == 2");
			Assert.AreEqual(sm.godel[2].post, "6 == 6");

			Assert.AreEqual(sm.godel[3].name, "SM.Initial.Action_3");
			Assert.AreEqual(sm.godel[3].controlOnly, false);
			Assert.AreEqual(sm.godel[3].inv, "1 == 1");
			Assert.AreEqual(sm.godel[3].pre, "2 == 2");
			Assert.AreEqual(sm.godel[3].post, "4 == 4");

			Assert.AreEqual(sm.godel[4].name, "SM.Initial");
			Assert.AreEqual(sm.godel[4].controlOnly, false);
			Assert.AreEqual(sm.godel[4].inv, "1 == 1");
			Assert.AreEqual(sm.godel[4].pre, "2 == 2");
			Assert.AreEqual(sm.godel[4].post, "3 == 3");

			Assert.AreEqual(sm.godel[5].name, "SM");
			Assert.AreEqual(sm.godel[5].controlOnly, false);
			Assert.AreEqual(sm.godel[5].inv, "1 == 1");
			Assert.AreEqual(sm.godel[5].pre, "5 == 5");
			Assert.AreEqual(sm.godel[5].post, "4 == 4");
		}

		[Test]
		public void SimpleInvariant()
		{
			string xml = @"
<Peach>

	<DataModel name='DM'>
		<String value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel inv='self != None'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			var e = new Engine(null);
			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.singleIteration = true;
			e.startFuzzing(dom, config);
		}

		[Test]
		public void ErrorCompiling()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel inv='this is bad python'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var e = new Engine(null);

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;

			try
			{
				e.startFuzzing(dom, config);
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				Assert.True(ex.Message.StartsWith("Error compiling Godel inv expression for Action 'SM.Initial.Action'."));
			}
		}

		[Test]
		public void ErrorExecuting()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel inv='x.y == 1'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var e = new Engine(null);

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;

			try
			{
				e.startFuzzing(dom, config);
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				Assert.True(ex.Message.StartsWith("Error, Godel failed to execute post-inv expression for Action 'SM.Initial.Action'."));
			}
		}

		[Test]
		public void SimpleActionFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel inv='str(self.dataModel.find(""str"").InternalValue) == ""Hello World""'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post-inv expression for Action 'SM.Initial.Action' failed.", faults[0].description);
		}

		[Test]
		public void SimpleStateFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Godel inv='str(self.actions[0].dataModel.find(""str"").InternalValue) == ""Hello World""'/>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post-inv expression for State 'SM.Initial' failed.", faults[0].description);
		}

		[Test]
		public void SimpleStateModelFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<Godel inv='str(self.states[0].actions[0].dataModel.find(""str"").InternalValue) == ""Hello World""'/>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post-inv expression for StateModel 'SM' failed.", faults[0].description);
		}

		[Test]
		public void SelfAndPreActionFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<Godel
					pre='str(self.dataModel.find(""str"").InternalValue) == ""Hello World""'
					post='str(self.dataModel.find(""str"").InternalValue) == str(pre.dataModel.find(""str"").InternalValue)'/>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post expression for Action 'SM.Initial.Action' failed.", faults[0].description);
		}

		[Test]
		public void SelfAndPreStateFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Godel
				pre='str(self.actions[0].dataModel.find(""str"").InternalValue) == ""Hello World""'
				post='str(self.actions[0].dataModel.find(""str"").InternalValue) == str(pre.actions[0].dataModel.find(""str"").InternalValue)'/>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post expression for State 'SM.Initial' failed.", faults[0].description);
		}

		[Test]
		public void SelfAndPreStateModelFault()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<Godel
			pre='str(self.states[0].actions[0].dataModel.find(""str"").InternalValue) == ""Hello World""'
			post='str(self.states[0].actions[0].dataModel.find(""str"").InternalValue) == str(pre.states[0].actions[0].dataModel.find(""str"").InternalValue)'/>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
";
			var faults = new List<Fault>();

			var e = new Engine(null);
			e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

			var parser = new Godel.Core.GodelPitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual("Godel post expression for StateModel 'SM' failed.", faults[0].description);
		}

		[Test]
		public void IncludedGodel()
		{
			var tmp1 = Path.GetTempFileName();
			var tmp2 = Path.GetTempFileName();

			string dm_xml = @"
<Peach>
	<Godel name='godel1' inv='str(self.dataModel.find(""str"").InternalValue) == ""Hello World""'/>

	<DataModel name='DM'>
		<String name='str' value='Hello World'/>
	</DataModel>
</Peach>
";

			string sm_xml = @"
<Peach>
	<Include ns='other' src='{0}'/>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='other:DM'/>
				<Godel ref='other:godel1'/>
			</Action>
		</State>
	</StateModel>
</Peach>
".Fmt(tmp1);

			string xml = @"
<Peach>
	<Include ns='sm' src='{0}'/>

	<Test name='Default'>
		<StateModel ref='sm:SM'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringMutator'/>
		</Mutators>
	</Test>
</Peach>
".Fmt(tmp2);

			File.WriteAllText(tmp1, dm_xml);
			File.WriteAllText(tmp2, sm_xml);

			try
			{
				var faults = new List<Fault>();

				var e = new Engine(null);
				e.Fault += (ctx, iter, stateModel, faultData) => { faults.AddRange(faultData); };

				var parser = new Godel.Core.GodelPitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Godel.Core.Dom;
				var config = new RunConfiguration();
				config.range = true;
				config.rangeStart = 1;
				config.rangeStop = 1;
				e.startFuzzing(dom, config);

				Assert.AreEqual(1, faults.Count);
				Assert.AreEqual("Godel post-inv expression for Action 'sm:SM.Initial.Action' failed.", faults[0].description);
			}
			finally
			{
				File.Delete(tmp1);
				File.Delete(tmp2);
			}
		}
	}
}

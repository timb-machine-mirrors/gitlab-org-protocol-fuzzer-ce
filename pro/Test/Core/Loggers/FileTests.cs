using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Loggers
{
	[TestFixture]
	class FileTests
	{
		class ExceptionalPublisher : Publisher
		{
			static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

			Engine engine;

			public ExceptionalPublisher(Engine engine, Dictionary<string, Variant> args)
				: base(args)
			{
				this.engine = engine;
			}

			protected override void OnOutput(Peach.Core.IO.BitwiseStream data)
			{
			}

			protected override Variant OnCall(string method, List<ActionParameter> args)
			{
				if (engine.context.controlIteration && !engine.context.controlRecordingIteration)
				{
					if (method == "throw")
						throw new SoftException("Erroring on a control iteration");

					return new Variant(new BitStream(Encoding.ASCII.GetBytes("false")));
				}

				return new Variant(new BitStream(Encoding.ASCII.GetBytes("true")));
			}

			protected override NLog.Logger Logger
			{
				get { return logger; }
			}
		}

		string logDir;

		[SetUp]
		public void SetUp()
		{
			logDir = Path.GetTempFileName();

			File.Delete(logDir);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(logDir))
				Directory.Delete(logDir, true);

			logDir = null;
		}

		[Test]
		public void TestSoftException()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='throw'>
				<Param>
					<DataModel ref='DM' />
				</Param>
			</Action>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
		<Logger class='File'>
			<Param name='Path' value='{0}' />
		</Logger>
	</Test>
</Peach>
".Fmt(logDir);

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			dom.tests[0].publishers[0] = new ExceptionalPublisher(e, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration()
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
				pitFile = "FileTests"
			};

			e.startFuzzing(dom, cfg);

			var subdirs = Directory.EnumerateDirectories(logDir).ToList();
			Assert.AreEqual(1, subdirs.Count);
			var root = subdirs[0];

			var faultDir = Path.Combine(root, "Faults", "ControlIteration", "3");
			Assert.True(Directory.Exists(faultDir));
			var desc = Path.Combine(faultDir, "PeachControlIteration.description.txt");
			Assert.True(File.Exists(desc));
			var asStr = File.ReadAllText(desc);

			Assert.True(asStr.Contains("Erroring on a control iteration"));
		}

		[Test]
		public void TestMissedActions()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action name='Action_X1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
		<Logger class='File'>
			<Param name='Path' value='{0}' />
		</Logger>
	</Test>
</Peach>
".Fmt(logDir);

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			dom.tests[0].publishers[0] = new ExceptionalPublisher(e, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration()
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
				pitFile = "FileTests"
			};

			e.startFuzzing(dom, cfg);

			var subdirs = Directory.EnumerateDirectories(logDir).ToList();
			Assert.AreEqual(1, subdirs.Count);
			var root = subdirs[0];

			var faultDir = Path.Combine(root, "Faults", "ControlIteration", "3");
			Assert.True(Directory.Exists(faultDir));
			var desc = Path.Combine(faultDir, "PeachControlIteration.description.txt");
			Assert.True(File.Exists(desc));
			var asStr = File.ReadAllText(desc);

			Assert.True(asStr.Contains("The following actions were not performed"));
			Assert.True(asStr.Contains("Action_X1"));
			Assert.True(asStr.Contains("Action_X2"));
			Assert.True(asStr.Contains("Action_X3"));
		}

		[Test]
		public void TestMissedStates()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action type='changeState' ref='State_True_1' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""' />
			<Action type='changeState' ref='State_False_1' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""' />
		</State>

		<State name='State_True_1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='changeState' ref='State_True_2' />
		</State>

		<State name='State_True_2'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>

		<State name='State_False_1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='changeState' ref='State_False_2' />
		</State>

		<State name='State_False_2'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
		<Logger class='File'>
			<Param name='Path' value='{0}' />
		</Logger>
	</Test>
</Peach>
".Fmt(logDir);

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			dom.tests[0].publishers[0] = new ExceptionalPublisher(e, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration()
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
				pitFile = "FileTests"
			};

			e.startFuzzing(dom, cfg);

			var subdirs = Directory.EnumerateDirectories(logDir).ToList();
			Assert.AreEqual(1, subdirs.Count);
			var root = subdirs[0];

			var faultDir = Path.Combine(root, "Faults", "ControlIteration", "3");
			Assert.True(Directory.Exists(faultDir));
			var desc = Path.Combine(faultDir, "PeachControlIteration.description.txt");
			Assert.True(File.Exists(desc));
			var asStr = File.ReadAllText(desc);

			Assert.True(asStr.Contains("The following states were not performed"));
			Assert.True(asStr.Contains("State_True_1"));
			Assert.True(asStr.Contains("State_True_2"));
		}
	}
}

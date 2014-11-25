using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.Agent.Monitors;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Category("Peach")]
	class SaveFileMonitorTests
	{
		private Fault[] faults;

		[Test]
		[ExpectedException(typeof(PeachException), ExpectedMessage =
			"Monitor 'SaveFile' is missing required parameter 'Filename'."
		)]
		public void TestNoParams()
		{
			new SaveFileMonitor(null, "", new Dictionary<string, Variant>());
		}

		[Test]
		public void TestCreate()
		{
			new SaveFileMonitor(null, "", new Dictionary<string, Variant>
				{
					{ "Filename", new Variant("c:\\some\\file") },
				}
			);
		}

		class Params : Dictionary<string, string> { }

		void OnFault(
			RunContext context,
			uint currentIteration,
			Peach.Core.Dom.StateModel stateModel,
			Fault[] faults)
		{
			Assert.Null(this.faults);
			this.faults = faults;
		}

		string MakeXml(Params parameters, bool fault)
		{
			string fmt = "<Param name='{0}' value='{1}'/>";

			string template = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='SaveFile'>
{0}
		</Monitor>
{1}
	</Agent>

	<Test name='Default' replayEnabled='false'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

			string faultSection = @"
		<Monitor class='FaultingMonitor'>
			<Param name='Iteration' value='C'/>
		</Monitor>
";
			var items = parameters.Select(kv => string.Format(fmt, kv.Key, kv.Value));
			var joined = string.Join(Environment.NewLine, items);
			var ret = string.Format(template, joined, fault ? faultSection : "");

			return ret;
		}

		private void Run(Params parameters, bool fault)
		{
			string xml = MakeXml(parameters, fault);

			PitParser parser = new PitParser();

			Dom dom = parser.asParser(null, new MemoryStream(
				Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)
			));
			dom.tests[0].includedMutators = new List<string> { "StringCaseMutator" };

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(null);
			e.Fault += OnFault;
			e.startFuzzing(dom, config);
		}

		[SetUp]
		public void Init()
		{
			faults = null;
		}

		[Test]
		public void TestNoFaults()
		{
			Run(new Params {
				{ "Filename", "Peach.Pro.Test.xml" },
			}, false);

			// verify values
			Assert.Null(faults);
		}

		[Test]
		public void TestFaults()
		{
			var filename = "Peach.Pro.Test.xml";
			Assert.That(() =>
				{
					Run(new Params {
						{ "Filename", filename },
					}, true);
				}, 
				Throws.TypeOf<PeachException>()
					.And.Message.EqualTo("Fault detected on control iteration.")
			);

			Assert.AreEqual(2, faults.Length);
			bool foundFault = false;
			foreach (var fault in faults)
			{
				if (fault.detectionSource == "SaveFileMonitor")
				{
					foundFault = true;
					Assert.AreEqual(1, fault.collectedData.Count);
					Assert.AreEqual(filename, fault.collectedData[0].Key);
				}
			}
			Assert.True(foundFault);
		}
	}
}

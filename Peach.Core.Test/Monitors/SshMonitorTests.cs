using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.IO;

namespace Peach.Core.Test.Monitors
{
	[TestFixture]
	class SshMonitorTests : DataModelCollector
	{
		class Params : Dictionary<string, string> { }

		const string TestHost = "";

		private Fault[] faults;

		[SetUp]
		public void Init()
		{
			if (string.IsNullOrEmpty(TestHost))
				Assert.Ignore("No test host configured.");

			faults = null;
		}

		void _Fault(RunContext context, uint currentIteration, Dom.StateModel stateModel, Fault[] faults)
		{
			Assert.Null(this.faults);
			this.faults = faults;
		}

		string MakeXml(Params parameters)
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
		<Monitor class='Ssh'>
{0}
		</Monitor>
	</Agent>

	<Test name='Default' replayEnabled='false'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

			var items = parameters.Select(kv => string.Format(fmt, kv.Key, kv.Value));
			var joined = string.Join(Environment.NewLine, items);
			var ret = string.Format(template, joined);

			return ret;
		}

		private void Run(Params parameters)
		{
			string xml = MakeXml(parameters);

			PitParser parser = new PitParser();

			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("StringCaseMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(null);
			e.Fault += _Fault;
			e.startFuzzing(dom, config);
		}

		[Test]
		public void TestNoFaults()
		{
			Run(new Params {
				{ "Host", TestHost },
				{ "Username", "test" },
				{ "Password", "test" },
				{ "Command", "ls" },
				{ "FaultOnMatch", "false" },
			});

			// verify values
			Assert.Null(faults);
		}

		void info_AuthenticationPrompt(object sender, Renci.SshNet.Common.AuthenticationPromptEventArgs e)
		{
			foreach (var prompt in e.Prompts)
			{
				prompt.Response = "test";
			}
		}

		int GetEventCount(Renci.SshNet.SshClient client)
		{
			var session = client.GetType().GetProperty("Session", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(client, null) as Renci.SshNet.Session;
			Assert.NotNull(session);

			var evt = session.GetType().GetEvent("ErrorOccured");
			var del = session.GetType().GetField(evt.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(session) as Delegate;

			return del.GetInvocationList().Length;
		}

		[Test]
		public void MultipleCommands()
		{
			//var ci = new Renci.SshNet.PasswordConnectionInfo("osxbuild-dev.local", "user", "password");
			var ci = new Renci.SshNet.KeyboardInteractiveConnectionInfo(TestHost, "test");
			ci.AuthenticationPrompt += new EventHandler<Renci.SshNet.Common.AuthenticationPromptEventArgs>(info_AuthenticationPrompt);

			using (var client = new Renci.SshNet.SshClient(ci))
			{
				client.Connect();

				var before = GetEventCount(client);
				for (var i = 0; i < 100; ++i)
				{
					using (var cmd = client.CreateCommand("echo hi"))
					{
						cmd.Execute();
					}
				}
				var after = GetEventCount(client);
			}
		}

		[Test]
		public void TestFaults()
		{
			try
			{
				Run(new Params {
					{ "Host", TestHost },
					{ "Username", "test" },
					{ "Password", "test" },
					{ "Command", "echo hello world hello" },
					{ "CheckValue", "hello.*?hello" },
					{ "FaultOnMatch", "true" },
				});

				Assert.Fail("Should have thrown.");
			}
			catch (PeachException ex)
			{
				Assert.AreEqual(ex.Message, "Fault detected on control iteration.");
			}

			// verify values
			Assert.NotNull(faults);
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("hello world hello\n", faults[0].description);
		}

		[Test, ExpectedException(typeof(PeachException))]
		public void TestBadPassword()
		{
			Run(new Params {
				{ "Host", TestHost },
				{ "Username", "test" },
				{ "Password", "badpassword" },
				{ "Command", "ls" },
				{ "FaultOnMatch", "false" },
			});
		}

		[Test, ExpectedException(typeof(PeachException))]
		public void TestRegex()
		{
			Run(new Params {
				{ "Host", TestHost },
				{ "Username", "test" },
				{ "Password", "test" },
				{ "Command", "ls" },
				{ "CheckValue", "(" },
			});
		}
	}
}

// end

using System.Configuration;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Test.Core.Agent.Http;
using Peach.Pro.Test.Core.Publishers;
using Encoding = Peach.Core.Encoding;
using Logger = NLog.Logger;


namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	[Quick]
	[Peach]
	class HttpChannelTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void HttpChannelPublisherTest()
		{
			var xml =
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
				"<Peach>\n" +
				"\t<DataModel name=\"Example1\">\n" +
				"\t\t<String value=\"Testing\" />\n" +
				"\t</DataModel>\n" +
				"\n" +
				"\t<StateModel name=\"TheStateModel\" initialState=\"initial\">\n" +
				"\t<State name=\"initial\">\n" +
				"\t  <Action type=\"output\">\n" +
				"\t\t<DataModel ref=\"Example1\"/>\n" +
				"\t  </Action>\n" +
				"\t</State>\n" +
				"\t</StateModel>\n" +
				"\n" +
				"\t<Agent name=\"TheAgent\" location=\"http://127.0.0.1:9000\">\n" +
				"\t\t<Monitor class=\"SaveFile\">\n" +
				"\t\t\t<Param name=\"Filename\" value=\"foo.txt\" />\n" +
				"\t\t</Monitor>\n" +
				"\t</Agent>\n" +
				"  \n" +
				"\t<Test name=\"Default\" maxOutputSize=\"200\">\n" +
				"\t\t<Agent ref=\"TheAgent\" />\n" +
				"\t\t<StateModel ref=\"TheStateModel\"/>\n" +
				"\t\t<Publisher class=\"Remote\">\n" +
				"\t\t\t<Param name=\"Agent\" value=\"TheAgent\" />\n" +
				"\t\t\t<Param name=\"Class\" value=\"ConsoleHex\"/>\n" +
				"\t\t</Publisher>\n" +
				"\t</Test>\n" +
				"</Peach>\n" +
				"";

			var expected = new string[]
			{
				"/Agent/AgentConnect",
				"/Agent/StartMonitor?name=Monitor&cls=SaveFile",
				"/Agent/SessionStarting",
				"/Agent/IterationStarting?iterationCount=0&isReproduction=False",
				"/Agent/Publisher/CreatePublisher",
				"/Agent/Publisher/start",
				"/Agent/Publisher/Set_Iteration",
				"/Agent/Publisher/Set_IsControlIteration",
				"/Agent/Publisher/open",
				"/Agent/Publisher/output",
				"/Agent/Publisher/close",
				"/Agent/IterationFinished",
				"/Agent/DetectedFault",
				"/Agent/Publisher/stop",
				"/Agent/SessionFinished",
				"/Agent/StopAllMonitors",
				"/Agent/AgentDisconnect"
			};
			
			var started = new AutoResetEvent(false);
			var stopped = new AutoResetEvent(false);

			var server = new HttpServer();
			server.Started += (sender, args) => started.Set();
			server.Started += (sender, args) => stopped.Set();
			var agentThread = new Thread(() =>
			{
				server.Run(9000, 9500);
			});

			agentThread.Start();
			Assert.IsTrue(started.WaitOne(60000));

			var dom = ParsePit(xml);
			dom.tests[0].agents[0].location = "http://127.0.0.1:" + server.Uri.Port + "/";

			var config = new RunConfiguration()
			{
				singleIteration = true
			};

			var e = new Engine(null);

			e.startFuzzing(dom, config);

			// Verify calls are made in correct order and all calls are made

			Assert.AreEqual(expected.Length, server.RestCalls.Count);
			for (var cnt = 0; cnt < expected.Length; cnt++)
				Assert.AreEqual(expected[cnt], server.RestCalls[cnt]);

			server.Stop();
			Assert.IsTrue(stopped.WaitOne(60000));
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NLog;
using NLog.Targets;
using NLog.Config;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Godel
{
	class AssertTestFail : System.Diagnostics.TraceListener
	{
		public override void Write(string message)
		{
			Assert.Fail(message);
		}

		public override void WriteLine(string message)
		{
			var sb = new System.Text.StringBuilder();

			sb.AppendLine("Assertion " + message);
			sb.AppendLine(new System.Diagnostics.StackTrace(2, true).ToString());

			Assert.Fail(sb.ToString());
		}
	}

	[SetUpFixture]
	class TestBase
	{
		[SetUp]
		public void Initialize()
		{
			System.Diagnostics.Debug.Listeners.Insert(0, new AssertTestFail());

			ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
			consoleTarget.Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}";

			LoggingConfiguration config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			LoggingRule rule = new LoggingRule("*", LogLevel.Info, consoleTarget);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}
	}
}

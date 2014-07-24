using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using NLog;
using NLog.Targets;
using NLog.Config;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.IO;
using System.Text.RegularExpressions;

namespace Peach
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

			var consoleTarget = new ConsoleTarget();
			consoleTarget.Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}";

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var rule = new LoggingRule("*", LogLevel.Info, consoleTarget);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}

		[TearDown]
		public void TearDown()
		{
			LogManager.Flush();
			LogManager.Configuration = null;
		}
	}


	[TestFixture]
	class AssertTest
	{
		[Test]
		public void TestAssert()
		{
#if DEBUG
			Assert.Throws<AssertionException>(delegate() {
				System.Diagnostics.Debug.Assert(false);
			});
#else
			System.Diagnostics.Debug.Assert(false);
#endif

		}
	}
}

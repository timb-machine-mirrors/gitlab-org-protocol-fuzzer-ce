using System.Diagnostics;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Peach.Core.Test
{
	class AssertTestFail : TraceListener
	{
		public override void Write(string message)
		{
			Assert.Fail(message);
		}

		public override void WriteLine(string message)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Assertion " + message);
			sb.AppendLine(new StackTrace(2, true).ToString());

			Assert.Fail(sb.ToString());
		}
	}

	[SetUpFixture]
	class TestBase
	{
		[SetUp]
		public void Initialize()
		{
			Debug.Listeners.Insert(0, new AssertTestFail());

			var consoleTarget = new ConsoleTarget
			{
				Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var rule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
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


	[TestFixture] [Category("Peach")]
	class AssertTest
	{
		[Test]
		public void TestAssert()
		{
#if DEBUG
			Assert.Throws<AssertionException>(() => Debug.Assert(false));
#else
			Debug.Assert(false);
#endif

		}
	}
}

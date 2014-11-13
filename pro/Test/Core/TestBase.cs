using System;
using System.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

// ReSharper disable once CheckNamespace (required for NUnit SetupFixture)
namespace Peach
{
	class AssertTestFail : TraceListener
	{
		public override void Write(string message)
		{
			Assert.Fail(message);
		}

		public override void WriteLine(string message)
		{
			var sb = new System.Text.StringBuilder();

			sb.AppendLine("Assertion " + message);
			sb.AppendLine(new StackTrace(2, true).ToString());

			Assert.Fail(sb.ToString());
		}
	}

	[SetUpFixture]
	class TestBase
	{
		public static ushort MakePort(ushort min, ushort max)
		{
			var pid = Process.GetCurrentProcess().Id;
			var seed = Environment.TickCount * pid;
			var rng = new Core.Random((uint)seed);
			var ret = (ushort)rng.Next(min, max);
			return ret;
		}

		static TestBase()
		{
			Debug.Listeners.Insert(0, new AssertTestFail());

			if (!(LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0))
			{
				var consoleTarget = new ConsoleTarget {Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"};

				var config = new LoggingConfiguration();
				config.AddTarget("console", consoleTarget);

				var rule = new LoggingRule("*", LogLevel.Info, consoleTarget);
				config.LoggingRules.Add(rule);

				LogManager.Configuration = config;
			}

			Core.Runtime.Program.LoadPlatformAssembly();
		}

		[SetUp]
		public void Initialize()
		{
		}
	}


	[TestFixture]
	[Category("Peach")]
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

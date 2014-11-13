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
using System.Diagnostics;

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
			int pid = Process.GetCurrentProcess().Id;
			int seed = Environment.TickCount * pid;
			var rng = new Peach.Core.Random((uint)seed);
			var ret = (ushort)rng.Next(min, max);
			return ret;
		}

		static TestBase()
		{
			Debug.Listeners.Insert(0, new AssertTestFail());

			if (!(LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0))
			{
				var consoleTarget = new ConsoleTarget();
				consoleTarget.Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}";

				var config = new LoggingConfiguration();
				config.AddTarget("console", consoleTarget);

				var rule = new LoggingRule("*", LogLevel.Info, consoleTarget);
				config.LoggingRules.Add(rule);

				LogManager.Configuration = config;
			}

			Peach.Core.Runtime.Program.LoadPlatformAssembly();
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
			Assert.Throws<AssertionException>(delegate()
			{
				Debug.Assert(false);
			});
#else
			Debug.Assert(false);
#endif

		}
	}
}

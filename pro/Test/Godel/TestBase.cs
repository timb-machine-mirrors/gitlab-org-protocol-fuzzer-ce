using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;
using NLog.Targets;
using NLog.Config;

using NUnit.Framework;
using System;
using Peach.Core;
using Peach.Core.Test;

namespace Godel
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

			var consoleTarget = new ColoredConsoleTarget
			{
				Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var logLevel = LogLevel.Info;
			var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
			if (peachTrace == "1")
				logLevel = LogLevel.Trace;

			var rule = new LoggingRule("*", logLevel, consoleTarget);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}
	}

	[TestFixture]
	[Quick]
	class CategoryTest
	{
		[Test]
		public void NoneMissing()
		{
			var missing = new List<string>();

			foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (!type.GetAttributes<TestFixtureAttribute>().Any())
					continue;

				foreach (var attr in type.GetCustomAttributes(true))
				{
					if (attr is QuickAttribute || attr is SlowAttribute)
						goto Found;
				}

				missing.Add(type.FullName);

			Found:
				{ }
			}

			Assert.That(missing, Is.Empty);
		}
	}
}

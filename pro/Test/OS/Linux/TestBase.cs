using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;
using System;

namespace Peach.Pro.Test.OS.Linux
{
	[SetUpFixture]
	class TestBase
	{
		[SetUp]
		public void Initialize()
		{
			// NUnit [Platform] attribute doesn't differentiate MacOSX/Linux
			if (Platform.GetOS() != Platform.OS.Linux)
				Assert.Ignore("Only supported on Linux");

			var consoleTarget = new ConsoleTarget
			{
				Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var logLevel = LogLevel.Debug;
			var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
			if (peachTrace == "1")
				logLevel = LogLevel.Trace;

			var rule = new LoggingRule("*", logLevel, consoleTarget);
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
}

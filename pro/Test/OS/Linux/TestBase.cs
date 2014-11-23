using NLog;
using NLog.Targets;
using NLog.Config;
using NUnit.Framework;
using Peach.Core;

// ReSharper disable once CheckNamespace
namespace Peach
{
	[SetUpFixture]
	public class TestBase
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

			var rule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
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

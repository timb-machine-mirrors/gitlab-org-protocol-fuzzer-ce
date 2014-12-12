using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;

namespace Peach.Pro.Test.OS.Windows
{
	[SetUpFixture]
	class TestBase
	{
		[SetUp]
		public void Initialize()
		{
			// NUnit [Platform] attribute doesn't differentiate MacOSX/Linux
			if (Platform.GetOS() != Platform.OS.Windows)
				Assert.Ignore("Only supported on Windows");

			var consoleTarget = new ColoredConsoleTarget
			{
				Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var rule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}
	}
}

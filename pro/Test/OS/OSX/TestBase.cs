using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;

namespace Peach.Pro.Test.OS.OSX
{
	[SetUpFixture]
	class TestBase
	{
		SingleInstance _si;

		[SetUp]
		public void Initialize()
		{
			// NUnit [Platform] attribute doesn't differentiate MacOSX/Linux
			if (Platform.GetOS() != Platform.OS.OSX)
				Assert.Ignore("Only supported on MacOSX");

			var consoleTarget = new ColoredConsoleTarget
			{
				Layout = "${date:format=HH\\:MM\\:ss} ${logger} ${message}"
			};

			var config = new LoggingConfiguration();
			config.AddTarget("console", consoleTarget);

			var rule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;

			// Ensure only 1 instance of the platform tests runs at a time
			_si = SingleInstance.CreateInstance("Peach.Pro.Test.OS.OSX.dll");
			_si.Lock();
		}

		[TearDown]
		public void TearDown()
		{
			if (_si != null)
			{
				_si.Dispose();
				_si = null;
			}
		}
	}
}

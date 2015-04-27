using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;
using System;
using Peach.Core.Test;

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

			var logLevel = LogLevel.Debug;
			var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
			if (peachTrace == "1")
				logLevel = LogLevel.Trace;

			var rule = new LoggingRule("*", logLevel, consoleTarget);
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

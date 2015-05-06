using System;
using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;
//using Peach.Pro.Core;
//using Peach.Pro.Core.Runtime;
using Peach.Core.Test;
using NLog.Targets.Wrappers;

namespace Peach.Pro.Test.WebApi
{
	class AssertTestFail : AssertWriter
	{
		protected override void OnAssert(string message)
		{
			Assert.Fail(message);
		}
	}

	[SetUpFixture]
	class TestBase
	{
		//TempDirectory _tmpDir;

		[SetUp]
		public void Initialize()
		{
			AssertWriter.Register<AssertTestFail>();

			if (!(LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0))
			{
				var consoleTarget = new AsyncTargetWrapper(new ConsoleTarget
				{
					Layout = "${time} ${logger} ${message}"
				});

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

			//Program.LoadPlatformAssembly();

			//_tmpDir = new TempDirectory();
			//Configuration.LogRoot = _tmpDir.Path;
		}

		[TearDown]
		public void TearDown()
		{
			//_tmpDir.Dispose();
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

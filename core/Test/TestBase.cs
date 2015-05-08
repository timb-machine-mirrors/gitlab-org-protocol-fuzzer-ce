﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using System;

namespace Peach.Core.Test
{
	public class PeachAttribute : CategoryAttribute { }
	public class QuickAttribute : CategoryAttribute { }
	public class SlowAttribute : CategoryAttribute { }

	public class SetUpFixture
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

		[SetUp]
		public void SetUp()
		{
			Debug.Listeners.Insert(0, new AssertTestFail());

			if (!(LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0))
			{
				var consoleTarget = new ConsoleTarget
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

			OnSetUp();
		}

		[TearDown]
		public void TearDown()
		{
			OnTearDown();

			LogManager.Flush();
			LogManager.Configuration = null;
		}

		protected virtual void OnSetUp()
		{
		}

		protected virtual void OnTearDown()
		{
		}
	}

	public class TestFixture
	{
		[Test]
		public void AssertWorks()
		{
#if DEBUG
			Assert.Throws<AssertionException>(() => Debug.Assert(false));
#else
			Debug.Assert(false);
#endif
		}

		[Test]
		public void NoMissingAttributes()
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

	[SetUpFixture]
	class TestBase : SetUpFixture
	{
	}

	[TestFixture]
	[Quick]
	class CommonTests : TestFixture
	{
	}
}

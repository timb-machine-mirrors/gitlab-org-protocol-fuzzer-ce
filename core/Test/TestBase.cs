using System.Collections.Generic;
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

		protected void DoSetUp()
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

				var peachDebug = Environment.GetEnvironmentVariable("PEACH_DEBUG");
				if (peachDebug == "1")
					logLevel = LogLevel.Debug;

				var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
				if (peachTrace == "1")
					logLevel = LogLevel.Trace;

				var rule = new LoggingRule("*", logLevel, consoleTarget);
				config.LoggingRules.Add(rule);

				var peachLog = Environment.GetEnvironmentVariable("PEACH_LOG");
				if (!string.IsNullOrEmpty(peachLog))
				{
					var fileTarget = new FileTarget
					{
						Name = "FileTarget",
						Layout = "${longdate} ${logger} ${message}",
						FileName = peachLog,
						Encoding = System.Text.Encoding.UTF8,
					};
					config.AddTarget("file", fileTarget);
					config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));
				}

				LogManager.Configuration = config;
			}
		}

		protected void DoTearDown()
		{
			LogManager.Flush();
			LogManager.Configuration = null;
		}
	}

	public abstract class TestFixture
	{
		readonly Assembly _asm;

		protected TestFixture(Assembly asm) { _asm = asm; }

		protected void DoAssertWorks()
		{
#if DEBUG
			Assert.Throws<AssertionException>(() => Debug.Assert(false));
#else
			Debug.Assert(false);
#endif
		}

		protected void DoNoMissingAttributes()
		{
			var missing = new List<string>();

			foreach (var type in _asm.GetTypes())
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
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}
}

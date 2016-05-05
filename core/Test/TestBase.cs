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
using System.Net;
using System.Net.Sockets;

namespace Peach.Core.Test
{
	public class PeachAttribute : CategoryAttribute { }
	public class QuickAttribute : CategoryAttribute { }
	public class SlowAttribute : CategoryAttribute { }

	public class SetUpFixture
	{
		public static ushort MakePort(ushort min, ushort max)
		{
			var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
			var seed = Environment.TickCount * pid;
			var rng = new Random((uint)seed);

			while (true)
			{
				var ret = (ushort)rng.Next(min, max);

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				return ret;
			}
		}

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

			OnSetUp();
		}

		public static void EnableDebug()
		{
			var config = LogManager.Configuration;
			var target = new ConsoleTarget { Layout = "${logger} ${message}" };
			var rule = new LoggingRule("*", LogLevel.Debug, target);
			
			config.AddTarget("debugConsole", target);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
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

	public abstract class TestFixture
	{
		readonly Assembly _asm;

		protected TestFixture(Assembly asm) { _asm = asm; }

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
	class TestBase : SetUpFixture
	{
	}

	[TestFixture]
	[Quick]
	class CommonTests : TestFixture
	{
		public CommonTests() : base(Assembly.GetExecutingAssembly()) { }
	}
}

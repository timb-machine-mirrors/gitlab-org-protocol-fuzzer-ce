using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture] [Category("Peach")]
	class RunCommandTests
	{
		[DllImport("libc")]
		private static extern int chmod(string path, int mode);

		private string _scriptFile;
		private string _outputFile;

		[SetUp]
		public void SetUp()
		{
			_outputFile = Path.GetTempFileName();
			_scriptFile = _outputFile + ".cmd";

			using (var f = new StreamWriter(_scriptFile))
			{
				if (Platform.GetOS() != Platform.OS.Windows)
				{
					chmod(_scriptFile, Convert.ToInt32("777", 8));
					f.WriteLine("#!/usr/bin/env sh");
					f.WriteLine("echo $* >> {0}", _outputFile);
				}
				else
				{
					f.WriteLine("echo %* >> {0}", _outputFile);
				}
			}
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(_outputFile);
			File.Delete(_scriptFile);
		}

		private MonitorRunner MakeWhen(string when)
		{
			return new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", when },
				{ "When", when },
			});
		}

		private void AddCall(MonitorRunner runner, string startOnCall)
		{
			runner.Add("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", "OnCall" },
				{ "When", "OnCall" },
				{ "StartOnCall", startOnCall },
			});
		}

		private void Verify(string expected)
		{
			var lines = File.ReadAllText(_outputFile);
			Assert.AreEqual(expected, lines.Trim());
		}

		private void VerifyLines(IEnumerable<string> expected)
		{
			var lines = File.ReadAllLines(_outputFile).Select(s => s.Trim());
			Assert.That(expected, Is.EqualTo(lines));
		}

		private void VerifyCall(int index)
		{
			switch (index)
			{
				case 0:
					VerifyLines(new string[0]);
					break;
				case 1:
					VerifyLines(new[] { "CallOne" });
					break;
				case 2:
					VerifyLines(new[] { "CallOne", "CallTwo" });
					break;
				case 3:
					VerifyLines(new[] { "CallOne", "CallTwo", "CallThree" });
					break;
				default:
					Assert.Fail("Unexpected number of lines to verify");
					break;
			}
		}

		[Test]
		public void TestNoArgs()
		{
			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>());
			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"RunCommand\".  Monitor 'RunCommand' is missing required parameter 'Command'.";
			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestNoWhen()
		{
			var runner = MakeWhen("");
			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"RunCommand\".  Monitor 'RunCommand' could not set value type parameter 'When' to 'null'.";
			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestOnStart()
		{
			var runner = MakeWhen("OnStart");

			runner.SessionStarting = m =>
			{
				Verify("");

				m.SessionStarting();

				Verify("OnStart");
			};

			runner.Run();

			Verify("OnStart");
		}

		[Test]
		public void TestOnEnd()
		{
			var runner = MakeWhen("OnEnd");

			runner.SessionFinished = m =>
			{
				Verify("");

				m.SessionFinished();

				Verify("OnEnd");
			};

			runner.Run();

			Verify("OnEnd");
		}

		[Test]
		public void TestOnIterationStart()
		{
			var runner = MakeWhen("OnIterationStart");

			runner.IterationStarting = (m, args) =>
			{
				Verify("");

				m.IterationStarting(args);

				Verify("OnIterationStart");
			};

			runner.Run();

			Verify("OnIterationStart");
		}

		[Test]
		public void TestOnIterationEnd()
		{
			var runner = MakeWhen("OnIterationEnd");

			runner.IterationFinished = m =>
			{
				Verify("");

				m.IterationFinished();

				Verify("OnIterationEnd");
			};

			runner.Run();

			Verify("OnIterationEnd");
		}


		[Test]
		public void TestOnFault()
		{
			var runner = MakeWhen("OnFault");

			runner.DetectedFault = m =>
			{
				Assert.False(m.DetectedFault(), "Monitor should not have detected fault.");

				// Trigger fault to runner
				return true;
			};

			runner.GetMonitorData = m =>
			{
				Verify("");

				var ret = m.GetMonitorData();

				Verify("OnFault");

				return ret;
			};

			runner.Run();

			Verify("OnFault");
		}

		[Test]
		public void TestIterAfterFault()
		{
			var runner = MakeWhen("OnIterationStartAfterFault");
			var it = 0;

			runner.DetectedFault = m =>
			{
				Assert.False(m.DetectedFault(), "Monitor should not have detected fault.");

				// Trigger fault to runner
				return true;
			};

			runner.IterationStarting = (m, args) =>
			{
				Verify("");

				m.IterationStarting(args);

				// We fault on every iteration, so iteration two is the first iteration after fault
				var exp = ++it == 2 ? "OnIterationStartAfterFault" : "";

				Verify(exp);
			};

			runner.Run(2);

			Verify("OnIterationStartAfterFault");
		}

		[Test]
		public void TestMissingStartOnCall()
		{
			var runner = MakeWhen("OnCall");

			runner.Run();

			Verify("");
		}

		[Test]
		public void TestOnCall()
		{
			var runner = new MonitorRunner();

			AddCall(runner, "CallOne");
			AddCall(runner, "CallTwo");
			AddCall(runner, "CallThree");

			var idx = 0;

			runner.Message = m =>
			{
				VerifyCall(idx++);

				// Called for each monitor, but each monitor just listens to
				// the specific message, so send every message to every monitor
				m.Message("CallOne");
				m.Message("CallTwo");
				m.Message("CallThree");

				VerifyCall(idx);
			};
		}
	}
}

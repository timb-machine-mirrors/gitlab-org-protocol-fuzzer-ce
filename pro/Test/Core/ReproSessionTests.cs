using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture, Category("Peach")]
	class ReproSessionTests
	{
#if DISABLED
		private static uint[] RunTest(uint start, string faultIter, uint maxSearch = 100, string reproIter = "0")
		{
			const string template = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello World'/>
		<String value='Hello World'/>
		<String value='Hello World'/>
		<String value='Hello World'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='FaultingMonitor'>
			<Param name='Iteration' value='{0}'/>
			<Param name='Repro' value='{1}'/>
		</Monitor>
	</Agent>

	<Test name='Default' faultWaitTime='0' targetLifetime='session' maxBackSearch='{2}'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var xml = string.Format(template, faultIter, reproIter, maxSearch);

			var dom = DataModelCollector.ParsePit(xml);

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = start,
				rangeStop = 12,
			};

			var e = new Engine(null);

			var iterationHistory = new List<uint>();

			e.IterationStarting += (ctx, it, tot) => iterationHistory.Add(it);
			e.startFuzzing(dom, config);

			return iterationHistory.ToArray();
		}

		[Test]
		public void TestFirstSearch()
		{
			var actual = RunTest(0, "1");

			var expected = new uint[] {
				1,  // Control
				1,
				1,  // Initial replay
				2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestSecondSearch()
		{
			var actual = RunTest(0, "2");

			var expected = new uint[] {
				1,  // Control
				1, 2,
				2,  // Initial replay
				1,  // Move back 1
				3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestMiddleSearch()
		{
			var actual = RunTest(1, "10");

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
				10, // Initial replay
				9,  // Move back 1
				8,  // Move back 2
				6,  // Move back 4
				2,  // Move back 8
				1,  // Move back to beginning
				11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeSearch()
		{
			var actual = RunTest(6, "10");

			var expected = new uint[] {
				6,  // Control
				6, 7, 8, 9, 10,
				10, // Initial replay
				9,  // Move back 1
				8,  // Move back 2
				6,  // Move back 4
				11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeBegin()
		{
			var actual = RunTest(6, "6");

			var expected = new uint[] {
				6, // Control
				6, // Trigger replay
				6, // Only replay
				7, 8, 9, 10, 11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeMaxEqual()
		{
			var actual = RunTest(1, "10", 4);

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
				10, // Initial replay
				9,  // Move back 1
				8,  // Move back 2
				6,  // Move back 4
				11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeMaxLess()
		{
			var actual = RunTest(1, "10", 5);

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
				10, // Initial replay
				9,  // Move back 1
				8,  // Move back 2
				6,  // Move back 4
				11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeNotPastFaultOne()
		{
			var actual = RunTest(1, "3,4", 100, "3");

			var expected = new uint[] {
				1,  // Control
				1, 2,
				3, // Trigger replay
				3, // Repro
				4,
				4, // Initial Replay
				5, 6, 7, 8, 9, 10, 11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeNotPastFault()
		{
			var actual = RunTest(1, "3,10", 100, "3");

			var expected = new uint[] {
				1,  // Control
				1, 2,
				3, // Trigger replay
				3, // Repro
				4, 5, 6, 7, 8, 9, 10,
				10, // Initial replay
				9,  // Move back 1
				8,  // Move back 2
				6,  // Move back 4
				4,  // Move back 6
				11, 12 };

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRangeNotPastFault2()
		{
			var actual = RunTest(1, "5", 100, "3");

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4,
				5, // Trigger replay
				5, // Initial replay
				4,
				3, // Repro
				4,
				5, // Trigger Replay
				5,
				4, // Repro failed
				6, 7, 8, 9, 10, 11, 12 };

			Assert.AreEqual(expected, actual);
		}

		private static void RunWaitTime(string waitTime, double min, double max)
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob name='blob1'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' waitTime='{0}'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(waitTime);

			var ticks = new List<int>();

			var dom = DataModelCollector.ParsePit(xml);

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 2
			};

			var e = new Engine(null);
			e.IterationStarting += (ctx, it, tot) => ticks.Add(Environment.TickCount);
			e.startFuzzing(dom, config);

			// verify values
			// Measure the first fuzzing iteration since a control iteration
			// will loose time for determining element mutability.
			Assert.AreEqual(3, ticks.Count);

			var delta = (ticks[2] - ticks[1]) / 1000.0;

			Assert.GreaterOrEqual(delta, min);
			Assert.LessOrEqual(delta, max);
		}

		[Test]
		public void TestWaitTimeOld()
		{
			RunWaitTime("2", 1.9, 2.1);
			RunWaitTime("0.1", 0.09, 0.11);
		}
#endif

		class Args
		{
			public Args()
			{
				RangeStart = 0;
				RangeStop = 10;

				ControlIterations = 0;

				WaitTime = 0.0;
				FaultWaitTime = 0.0;
			}

			public uint RangeStart { get; set; }
			public uint RangeStop { get; set; }

			public uint ControlIterations { get; set; }

			public double WaitTime { get; set; }
			public double FaultWaitTime { get; set; }

			public string Initial { get; set; }
			public string Fault { get; set; }
			public string Repro { get; set; }

			public uint? MaxBackSearch { get; set; }
		}

		private readonly List<TimeSpan> _waitTimes = new List<TimeSpan>();

		[SetUp]
		public void SetUp()
		{
			_waitTimes.Clear();
		}


		private string Run(Args args)
		{
			var max = args.MaxBackSearch.HasValue ? "maxBackSearch='{0}'".Fmt(args.MaxBackSearch) : "";

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello World'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='FaultingMonitor'>
			<Param name='Iteration' value='0'/>
			<Param name='Repro' value='0'/>
		</Monitor>
	</Agent>

	<Test name='Default' waitTime='{0}' faultWaitTime='{1}' controlIteration='{2}' {3}>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(args.WaitTime, args.FaultWaitTime, args.ControlIterations, max);

			var dom = DataModelCollector.ParsePit(xml);

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = args.RangeStart,
				rangeStop = args.RangeStop,
			};

			var history = new List<string>();

			var e = new Engine(null);

			var sw = new Stopwatch();

			e.TestStarting += ctx =>
			{
				ctx.DetectedFault += (c, agent) => _waitTimes.Add(sw.Elapsed);
			};

			e.IterationStarting += (ctx, it, ti) =>
			{
				string i;

				if (ctx.controlRecordingIteration)
					i = "R" + it;
				else if (ctx.controlIteration)
					i = "C" + it;
				else
					i = it.ToString(CultureInfo.InvariantCulture);

				history.Add(i);

				if (!ctx.reproducingFault && i == args.Fault)
					ctx.agentManager.Message("Fault", new Variant("true"));
				else if (ctx.reproducingFault && i == args.Repro)
					ctx.agentManager.Message("Fault", new Variant("true"));
				else if (i == args.Initial)
					ctx.agentManager.Message("Fault", new Variant("true"));
			};

			e.IterationFinished += (ctx, it) => sw.Restart();

			e.Fault += (ctx, it, sm, fault) =>
			{
				history.Add("Fault");

				Assert.AreEqual(1, fault.Length);
				Assert.LessOrEqual(it, ctx.reproducingInitialIteration);
				Assert.GreaterOrEqual(ctx.reproducingIterationJumpCount, 0);

			};
			e.ReproFault += (ctx, it, sm, fault) =>
			{
				history.Add("ReproFault");

				Assert.AreEqual(1, fault.Length);
				Assert.AreEqual(it, ctx.reproducingInitialIteration);
				Assert.AreEqual(0, ctx.reproducingIterationJumpCount);
			};

			e.ReproFailed += (ctx, it) =>
			{
				history.Add("ReproFailed");

				Assert.AreEqual(it, ctx.reproducingInitialIteration);
				Assert.Greater(ctx.reproducingIterationJumpCount, 0);
			};

			e.startFuzzing(dom, config);

			return string.Join(" ", history);
		}

		[Test]
		public void TestSimpleRepro()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 8
			// Repro on iteration 8

			var act = Run(new Args { Fault = "8", Repro = "8" });

			const string exp = "R1 1 2 3 4 5 6 7 8 ReproFault 8 Fault 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBack10()
		{
			// Fuzz from 1 to 15
			// Fault on iteration 12
			// Repro on iteration 11

			// On fault replays iteration 12
			// Jumps back 10 and goes forward until reproduces on 11
			// Resumes on iteration 13

			var act = Run(new Args { RangeStop = 15, Fault = "12", Repro = "11" });

			const string exp = "R1 1 2 3 4 5 6 7 8 9 10 11 12 ReproFault 12 2 3 4 5 6 7 8 9 10 11 Fault 13 14 15";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBack5()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 6
			// Repro on iteration 5

			// On fault replays iteration 6
			// Jumps back 5 and goes forward until reproduces on 5
			// Resumes on iteration 7

			var act = Run(new Args { Fault = "6", Repro = "5" });

			const string exp = "R1 1 2 3 4 5 6 ReproFault 6 1 2 3 4 5 Fault 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproLastFault()
		{
			// Fuzz from 1 to 20
			// Fault and repro on on iteration 4
			// Fault on iteration 18

			// On fault replays iteration 18
			// Jumps back 10 and fails to repro
			// Jumps back to iteration 5 (last fault was 4) and reproduces on iteration 7
			// Resumes on iteration 19

			var act = Run(new Args { RangeStop = 20, Fault = "18", Repro = "7", Initial = "4" });

			const string exp = "R1 1 2 3 4 ReproFault 4 Fault 5 6 7 8 9 10 11 12 13 14 15 16 17 18 " +
				"ReproFault 18 8 9 10 11 12 13 14 15 16 17 18 5 6 7 Fault 19 20";

			Assert.AreEqual(exp, act);
		}


		[Test]
		public void TestNoReproBack5()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 6
			// Never reproduce

			// On fault replays iteration 6
			// Jumps back 5 and goes forward until runs iteration 6 and says repro failed
			// Resumes on iteration 7

			var act = Run(new Args { Fault = "6" });

			const string exp = "R1 1 2 3 4 5 6 ReproFault 6 1 2 3 4 5 6 ReproFailed 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBackDouble()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", Repro = "10" });

			var exp = "R1";

			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFault 23";

			for (var i = 13; i <= 23; ++i)
				exp += " " + i;

			for (var i = 3; i <= 10; ++i)
				exp += " " + i;

			exp += " Fault 24 25";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestMaxBackSearch500()
		{
			// Set MaxBackSearch to 500
			// Fuzz from iteration 1 to 550
			// Fault on iteration 540 without repro
			// Should run 540, 530-540, keep doubling backlog until it maxes at 500

			var act = Run(new Args { RangeStop = 550, Fault = "540", MaxBackSearch = 500 });

			var sb = new StringBuilder();
			sb.Append("R1");

			for (var i = 1; i <= 540; ++i)
				sb.AppendFormat(" {0}", i);

			sb.Append(" ReproFault 540");

			for (var i = 10; i <= 640; i *= 2)
				for (var j = 540 - Math.Min(i, 500); j <= 540; ++j)
					sb.AppendFormat(" {0}", j);

			sb.Append(" ReproFailed");

			for (var i = 541; i <= 550; ++i)
				sb.AppendFormat(" {0}", i);

			var exp = sb.ToString();
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestMaxBackSearchDefault()
		{
			// Set MaxBackSearch to 500
			// Fuzz from iteration 1 to 550
			// Fault on iteration 540 without repro
			// Should run 540, 530-540, keep doubling backlog until it maxes at default of 80

			var act = Run(new Args { RangeStop = 550, Fault = "540" });

			var sb = new StringBuilder();
			sb.Append("R1");

			for (var i = 1; i <= 540; ++i)
				sb.AppendFormat(" {0}", i);

			sb.Append(" ReproFault 540");

			for (var i = 10; i <= 80; i *= 2)
				for (var j = 540 - i; j <= 540; ++j)
					sb.AppendFormat(" {0}", j);

			sb.Append(" ReproFailed");

			for (var i = 541; i <= 550; ++i)
				sb.AppendFormat(" {0}", i);

			var exp = sb.ToString();
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestWaitTime()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", WaitTime = 0.1 });

			var exp = "R1";

			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFault 23";

			for (var i = 13; i <= 23; ++i)
				exp += " " + i;

			for (var i = 3; i <= 23; ++i)
				exp += " " + i;

			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFailed 24 25";

			Assert.AreEqual(exp, act);

			// R1, 1..23, 23, 13..23, 3..23, 1..25
			const int iterations = 1 + 23 + 1 + 11 + 21 + 25;
			Assert.AreEqual(iterations, _waitTimes.Count);

			foreach (var ts in _waitTimes)
			{
				Assert.GreaterOrEqual(ts, TimeSpan.FromMilliseconds(80));
				Assert.LessOrEqual(ts, TimeSpan.FromMilliseconds(120));
			}
		}

		[Test]
		public void TestFaultWaitTime()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", FaultWaitTime = 0.1 });
			var times = new List<int>();

			var exp = "R1";
			times.Add(0);

			for (var i = 1; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0);
			}

			exp += " ReproFault 23";
			times.Add(100); // Waits FaultWaitTime after 1st repro

			for (var i = 13; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(100); // Waits FaultWaitTime after each of 1st 10 repros
			}

			for (var i = 3; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0); // Does not wait when doing scan past 10
			}

			times[times.Count - 1] = 100; // Waits FaultWaitTime after full sequence

			for (var i = 1; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0); // Does not wait when doing scan past 10
			}

			times[times.Count - 1] = 100; // Waits FaultWaitTime after full sequence

			exp += " ReproFailed 24 25";

			times.Add(0);
			times.Add(0);

			Assert.AreEqual(exp, act);

			Assert.AreEqual(times.Count, _waitTimes.Count);

			for (var i = 0; i < times.Count; ++i)
			{
				Assert.GreaterOrEqual(_waitTimes[i], TimeSpan.FromMilliseconds(times[i] - 20));
				Assert.LessOrEqual(_waitTimes[i], TimeSpan.FromMilliseconds(times[i] + 20));
			}
		}
	}
}
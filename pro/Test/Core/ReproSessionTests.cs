using System;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture, Category("Peach")]
	class ReproSessionTests
	{
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
		public void TestWaitTime()
		{
			RunWaitTime("2", 1.9, 2.1);
			RunWaitTime("0.1", 0.09, 0.11);
		}
	}
}
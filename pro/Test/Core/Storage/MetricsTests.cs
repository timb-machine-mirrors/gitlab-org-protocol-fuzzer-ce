using System;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using System.Linq;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	class MetricsTests
	{
		TempFile _tmp;
		DateTime _now;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempFile();
			_now = DateTime.UtcNow;

			using (var db = new JobContext(_tmp.Path))
			{
				var cache = new MetricsCache(_tmp.Path, db);

				cache.IterationStarting(1);
				cache.StateStarting("S1", 1);
				cache.StateStarting("S2", 1);
				cache.ActionStarting("A1");
				cache.ActionStarting("A2");
				cache.DataMutating("P1", "E1", "M1", "D1");
				cache.ActionStarting("A3");
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.DataMutating("P1", "E1", "M1", "D1");
				cache.DataMutating("P2", "E2", "M2", "D2");
				cache.IterationFinished();

				cache.IterationStarting(2);
				cache.StateStarting("S1", 1);
				cache.StateStarting("S2", 1);
				cache.ActionStarting("A1");
				cache.ActionStarting("A2");
				cache.DataMutating("P1", "E1", "M3", "D1");
				cache.IterationFinished();

				// simulate a reproducing iteration
				cache.IterationStarting(2);
				cache.StateStarting("S1", 1);
				cache.StateStarting("S2", 1);
				cache.ActionStarting("A1");
				cache.ActionStarting("A2");
				cache.DataMutating("P1", "E1", "M3", "D1");
				// no iteration because we're reproducing

				// reproduce on iteration 1
				cache.IterationStarting(1);
				cache.StateStarting("S1", 1);
				cache.StateStarting("S2", 1);
				cache.ActionStarting("A1");
				cache.ActionStarting("A2");
				cache.DataMutating("P1", "E1", "M1", "D1");
				cache.ActionStarting("A3");
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.OnFault(new FaultMetric
				{
					Iteration = 1,
					MajorHash = "AAA",
					MinorHash = "BBB",
					Timestamp = _now,
					Hour = _now.Hour,
				});

				cache.IterationStarting(3);
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.DataMutating("P3", "E3", "M3", "D3");
				cache.IterationFinished();
				cache.OnFault(new FaultMetric
				{
					Iteration = 3,
					MajorHash = "AAA",
					MinorHash = "BBB",
					Timestamp = _now,
					Hour = _now.Hour,
				});

				cache.IterationStarting(4);
				cache.StateStarting("S4", 1);
				cache.ActionStarting("A4");
				cache.DataMutating("P4", "E4", "M4", "D4");
				cache.IterationFinished();

				// repro iteration 4
				cache.IterationStarting(4);
				cache.StateStarting("S4", 1);
				cache.ActionStarting("A4");
				cache.DataMutating("P4", "E4", "M4", "D4");
				cache.ActionStarting("A5");
				cache.DataMutating("P4", "E5", "M4", "D4");
				cache.OnFault(new FaultMetric
				{
					Iteration = 4,
					MajorHash = "XXX",
					MinorHash = "YYY",
					Timestamp = _now + TimeSpan.FromHours(1),
					Hour = _now.Hour + 1,
				});

				cache.IterationStarting(5);
				cache.StateStarting("S5", 1);
				cache.ActionStarting("A5");
				cache.DataMutating("P5", "E5", "M5", "D5");
				cache.StateStarting("S5", 2);
				cache.ActionStarting("A5");
				cache.DataMutating("P5", "E5", "M5", "D5");
				cache.IterationFinished();
				cache.OnFault(new FaultMetric
				{
					Iteration = 5,
					MajorHash = "AAA",
					MinorHash = "YYY",
					Timestamp = _now + TimeSpan.FromHours(2),
					Hour = _now.Hour + 2,
				});

				cache.IterationStarting(6);
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.DataMutating("P3", "E3", "M3", "D3");
				cache.IterationFinished();
				cache.OnFault(new FaultMetric
				{
					Iteration = 6,
					MajorHash = "AAA",
					MinorHash = "BBB",
					Timestamp = _now + TimeSpan.FromHours(3),
					Hour = _now.Hour + 3,
				});

				cache.IterationStarting(7);
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.DataMutating("P3", "E3", "M3", "D3");
				cache.IterationFinished();

				cache.IterationStarting(8);
				cache.StateStarting("S3", 1);
				cache.ActionStarting("A3");
				cache.DataMutating("P3", "E3", "M3", "D3");
				cache.IterationFinished();
				cache.OnFault(new FaultMetric
				{
					Iteration = 8,
					MajorHash = "XXX",
					MinorHash = "YYY",
					Timestamp = _now + TimeSpan.FromHours(4),
					Hour = _now.Hour + 4,
				});
			}
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		[Test]
		public void TestQueryStates()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryStates(), new[]
				{
					new StateMetric("S1_1", 4),
					new StateMetric("S2_1", 4),
					new StateMetric("S3_1", 6),
					new StateMetric("S4_1", 2),
					new StateMetric("S5_1", 1),
					new StateMetric("S5_2", 1),
				});
			}
		}

		[Test]
		public void TestQueryIterations()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryIterations(), new[]
				{
					new IterationMetric("S2_1", "A2", "P1", "E1", "M1", "D1", 1),
					new IterationMetric("S3_1", "A3", "P1", "E1", "M1", "D1", 1),
					new IterationMetric("S3_1", "A3", "P2", "E2", "M2", "D2", 1),
					new IterationMetric("S2_1", "A2", "P1", "E1", "M3", "D1", 1),
					new IterationMetric("S4_1", "A4", "P4", "E4", "M4", "D4", 1),
					new IterationMetric("S5_1", "A5", "P5", "E5", "M5", "D5", 1),
					new IterationMetric("S5_2", "A5", "P5", "E5", "M5", "D5", 1),
					new IterationMetric("S3_1", "A3", "P3", "E3", "M3", "D3", 4),
				});
			}
		}

		[Test]
		public void TestQueryBuckets()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryBuckets(), new[]
				{
					new BucketMetric("AAA_BBB", "M1", "S2_1.A2.P1.E1", 1, 1),
					new BucketMetric("AAA_BBB", "M3", "S3_1.A3.P3.E3", 4, 2),
					new BucketMetric("AAA_YYY", "M5", "S5_1.A5.P5.E5", 1, 1),
					new BucketMetric("AAA_YYY", "M5", "S5_2.A5.P5.E5", 1, 1),
					new BucketMetric("XXX_YYY", "M3", "S3_1.A3.P3.E3", 4, 1),
					new BucketMetric("XXX_YYY", "M4", "S4_1.A4.P4.E4", 1, 1),
				});
			}
		}

		[Test]
		public void TestQueryBucketTimeline()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var data = db.QueryBucketTimeline();
				AssertResult(data, new[]
				{
					new BucketTimelineMetric("AAA_BBB", 1, _now, 1),
					new BucketTimelineMetric("AAA_YYY", 5, _now + TimeSpan.FromHours(2), 1),
					new BucketTimelineMetric("XXX_YYY", 4, _now + TimeSpan.FromHours(1), 1),
				});
			}
		}

		[Test]
		public void TestQueryMutator()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var data = db.QueryMutators();
				AssertResult(data, new[]
				{
					new MutatorMetric("M1", 2, 2, 1, 1),
					new MutatorMetric("M2", 1, 1, 0, 0),
					new MutatorMetric("M3", 2, 5, 2, 3),
					new MutatorMetric("M4", 1, 1, 1, 1),
					new MutatorMetric("M5", 2, 2, 1, 1),
				});
			}
		}

		[Test]
		public void TestQueryElement()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryElements(), new[]
				{
					new ElementMetric("S2_1", "A2", "P1", "D1", "E1", 2, 1, 1),
					new ElementMetric("S3_1", "A3", "P1", "D1", "E1", 1, 0, 0),
					new ElementMetric("S3_1", "A3", "P2", "D2", "E2", 1, 0, 0),
					new ElementMetric("S3_1", "A3", "P3", "D3", "E3", 4, 2, 3),
					new ElementMetric("S4_1", "A4", "P4", "D4", "E4", 1, 1, 1),
					new ElementMetric("S4_1", "A5", "P4", "D4", "E5", 1, 1, 1),
					new ElementMetric("S5_1", "A5", "P5", "D5", "E5", 1, 1, 1),
					new ElementMetric("S5_2", "A5", "P5", "D5", "E5", 1, 1, 1),
				});
			}
		}

		[Test]
		public void TestQueryDataset()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryDatasets(), new[]
				{
					new DatasetMetric("D1", 3, 1, 1),
					new DatasetMetric("D2", 1, 0, 0),
					new DatasetMetric("D3", 4, 2, 3),
					new DatasetMetric("D4", 1, 1, 1),
					new DatasetMetric("D5", 2, 1, 1),
				});
			}
		}

		[Test]
		public void TestQueryFaultTimeline()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryFaultTimeline(), new[]
				{
					new FaultTimelineMetric(_now, 2),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(1), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(2), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(3), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(4), 1),
				});
			}
		}

		void AssertResult<T>(IEnumerable<T> actual, IEnumerable<T> expected)
		{
			var actualList = actual.ToList();
			var expectedList = expected.ToList();

			JobContext.Dump(actualList);

			Assert.AreEqual(expectedList.Count, actualList.Count, "Rows mismatch");

			var type = typeof(T);
			for (var i = 0; i < actualList.Count; i++)
			{
				var actualRow = actualList[i];
				var expectedRow = expectedList[i];
				foreach (var pi in type.GetProperties()
					.Where(x => !x.HasAttribute<NotMappedAttribute>()))
				{
					var actualValue = pi.GetValue(actualRow, null);
					var expectedValue = pi.GetValue(expectedRow, null);
					var msg = "Values mismatch on row {0} column {1}.".Fmt(i, pi.Name);
					Assert.AreEqual(expectedValue, actualValue, msg);
				}
			}
		}
	}
}

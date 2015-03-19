using System;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Runtime.Enterprise;
using Peach.Pro.Core.Storage;
using System.Linq;
using Peach.Pro.Core.WebServices.Models;
using Action = Peach.Pro.Core.Storage.Action;
using Mutator = Peach.Pro.Core.Storage.Mutator;
using Parameter = Peach.Pro.Core.Storage.Parameter;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	class MetricsTests
	{
		TempFile _tmp;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempFile();
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
				var cache = new MetricCache<State>(db);
				var s1 = cache.Add(db, "S1");
				var s2 = cache.Add(db, "S2");

				for (var i = 0; i < 10; i++)
					db.InsertStateInstance(new StateInstance { StateId = s1 });
				for (var i = 0; i < 20; i++)
					db.InsertStateInstance(new StateInstance { StateId = s2 });
			}

			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryStates(), new[]
				{
					new StateMetric("S1", 10),
					new StateMetric("S2", 20),
				});
			}
		}

		[Test]
		public void TestQueryIterations()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);

				for (var i = 0; i < 10; i++)
				{
					inserter.AddMutations(i, new List<long[]>
					{
						new long[] {1, 1, 1, 1, 1, 1},
					});
				}

				for (var i = 0; i < 20; i++)
				{
					inserter.AddMutations(i, new List<long[]>
					{
						new long[] {1, 2, 1, 1, 1, 1},
					});
				}
			}

			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryIterations(), new[]
				{
					new IterationMetric("S1", "A1", "P1", "E1", "M1", "D1", 10),
					new IterationMetric("S2", "A1", "P1", "E1", "M1", "D1", 20),
				});
			}
		}

		[Test]
		public void TestQueryBuckets()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);
				inserter.AddData(DateTime.UtcNow);
			}

			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryBuckets(), new[]
				{
					new BucketMetric("AAA_BBB", "M0", "S0.A0.P0.E0", 5, 3),
					new BucketMetric("AAA_BBB", "M1", "S1.A1.P1.E1", 5, 3),
					new BucketMetric("XXX_YYY", "M0", "S0.A0.P0.E0", 5, 3),
					new BucketMetric("XXX_YYY", "M1", "S1.A1.P1.E1", 5, 3),
				});
			}
		}

		[Test]
		public void TestQueryBucketTimeline()
		{
			var start = DateTime.UtcNow;
			var next = start + TimeSpan.FromHours(2);

			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);
				inserter.AddData(start);
			}

			using (var db = new JobContext(_tmp.Path))
			{
				var data = db.QueryBucketTimeline();
				AssertResult(data, new[]
				{
					new BucketTimelineMetric("AAA_BBB", 3, start.ToLocalTime(), 1),
					new BucketTimelineMetric("XXX_YYY", 5, next.ToLocalTime(), 1),
				});
			}
		}

		[Test]
		public void TestQueryMutator()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);
				inserter.AddData(DateTime.UtcNow);
			}

			using (var db = new JobContext(_tmp.Path))
			{
				var data = db.QueryMutators();
				AssertResult(data, new[]
				{
					new MutatorMetric("M0", 1, 5, 2, 3),
					new MutatorMetric("M1", 1, 5, 2, 3),
				});
			}
		}

		[Test]
		public void TestQueryElement()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);
				inserter.AddData(DateTime.UtcNow);
			}

			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryElements(), new[]
				{
					new ElementMetric("S0", "A0", "P0", "D0", "E0", 5, 2, 3),
					new ElementMetric("S1", "A1", "P1", "D1", "E1", 5, 2, 3),
				});
			}
		}

		[Test]
		public void TestQueryDataset()
		{
			using (var db = new JobContext(_tmp.Path))
			{
				var inserter = new Inserter(db);
				inserter.AddData(DateTime.UtcNow);
			}

			using (var db = new JobContext(_tmp.Path))
			{
				AssertResult(db.QueryDatasets(), new[]
				{
					new DatasetMetric("D0", 5, 2, 3),
					new DatasetMetric("D1", 5, 2, 3),
				});
			}
		}

		void AssertResult<T>(IEnumerable<T> actual, IEnumerable<T> expected)
		{
			Dump(actual);

			var actualList = actual.ToList();
			var expectedList = expected.ToList();

			Assert.AreEqual(expectedList.Count, actualList.Count, "Rows mismatch");

			var type = typeof(T);
			for (int i = 0; i < actualList.Count; i++)
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

		class Inserter
		{
			readonly MetricsCache _cache;
			readonly JobContext _db;

			public Inserter(JobContext db)
			{
				_db = db;
				_cache = new MetricsCache(db);
			}

			Mutation NewMutation(int iteration, long[] x)
			{
				_cache.Add<Mutator>(_db, "M{0}".Fmt(x[0]));
				_cache.Add<State>(_db, "S{0}".Fmt(x[1]));
				_cache.Add<Action>(_db, "A{0}".Fmt(x[2]));
				_cache.Add<Parameter>(_db, "P{0}".Fmt(x[3]));
				_cache.Add<Element>(_db, "E{0}".Fmt(x[4]));
				_cache.Add<Dataset>(_db, "D{0}".Fmt(x[5]));

				return new Mutation
				{
					Iteration = iteration,
					MutatorId = x[0],
					StateId = x[1],
					ActionId = x[2],
					ParameterId = x[3],
					ElementId = x[4],
					DatasetId = x[5],
				};
			}

			public void AddMutations(int iteration, IEnumerable<long[]> mutationIds)
			{
				foreach (var x in mutationIds)
				{
					_db.InsertMutation(NewMutation(iteration, x));
				}
			}

			public void AddFault(
				int iteration,
				string majorHash,
				string minorHash,
				DateTime timestamp,
				IEnumerable<long[]> mutationIds)
			{
				var fault = new FaultMetric
				{
					Iteration = iteration,
					MajorHash = majorHash,
					MinorHash = minorHash,
					Timestamp = timestamp,
					Hour = timestamp.Hour,
				};
				var mutations = mutationIds.Select(x => NewMutation(iteration, x));
				_db.InsertFaultMetric(fault, mutations.ToList());
			}

			public void AddData(DateTime start)
			{
				AddMutations(1, new List<long[]>
				{
					new long[] {0, 0, 0, 0, 0, 0},
					new long[] {1, 1, 1, 1, 1, 1},
					new long[] {2, 2, 2, 2, 2, 2},
				});
				AddMutations(2, new List<long[]>
				{
					new long[] {0, 0, 0, 0, 0, 0},
					new long[] {1, 1, 1, 1, 1, 1},
					new long[] {2, 2, 2, 2, 2, 2},
				});
				AddFault(3, "AAA", "BBB", start, new List<long[]>
				{
					new long[] {0, 0, 0, 0, 0, 0},
					new long[] {1, 1, 1, 1, 1, 1},
					new long[] {2, 2, 2, 2, 2, 2},
				});
				AddFault(4, "AAA", "BBB", start + TimeSpan.FromHours(1), new List<long[]>
				{
					new long[] {0, 0, 0, 0, 0, 0},
					new long[] {1, 1, 1, 1, 1, 1},
					new long[] {2, 2, 2, 2, 2, 2},
				});
				AddFault(5, "XXX", "YYY", start + TimeSpan.FromHours(2), new List<long[]>
				{
					new long[] {0, 0, 0, 0, 0, 0},
					new long[] {1, 1, 1, 1, 1, 1},
					new long[] {2, 2, 2, 2, 2, 2},
				});
			}
		}

		void Dump<T>(IEnumerable<T> data)
		{
			var type = typeof(T);

			var columns = type.GetProperties()
				.Where(pi => !pi.HasAttribute<NotMappedAttribute>())
				.ToList();

			var maxWidth = new int[columns.Count];
			var header = new string[columns.Count];
			var rows = new List<string[]> { header };

			for (var i = 0; i < columns.Count; i++)
			{
				var pi = columns[i];
				header[i] = pi.Name;
				maxWidth[i] = pi.Name.Length;
			}

			foreach (var item in data)
			{
				var row = new string[columns.Count];
				var values = columns.Select(pi => pi.GetValue(item, null).ToString())
					.ToArray();
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					row[i] = value;
					maxWidth[i] = Math.Max(maxWidth[i], value.Length);
				}
				rows.Add(row);
			}

			var fmts = maxWidth
				.Select((t, i) => "{0},{1}".Fmt(i, t))
				.Select(fmt => "{" + fmt + "}")
				.ToList();
			var finalFmt = string.Join("|", fmts);
			foreach (object[] row in rows)
			{
				Console.WriteLine(finalFmt, row);
			}
		}
	}
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.Loggers
{
	[TestFixture]
	[Quick]
	[Peach]
	class JobLoggerTests
	{
		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
		<Logger class='File' />
		<Mutators mode='include'>
			<Mutator class='StringCaseRandom' />
		</Mutators>
	</Test>
</Peach>";

		[Test]
		public void TestRelativePaths()
		{
			// Job's Log path should be rooted
			// Fault's path should be relative to the Job's 
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (!ctx.controlIteration && ctx.currentIteration == 3)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});
				}
			};

			e.startFuzzing(dom, cfg);

			Job job;

			using (var db = new NodeDatabase())
			{
				var jobs = db.LoadTable<Job>().ToList();

				Assert.AreEqual(1, jobs.Count);

				job = jobs[0];
			}

			Assert.True(Path.IsPathRooted(job.LogPath), "Job's LogPath should be absolute");

			using (var db = new JobDatabase(job.DatabasePath, false))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.Greater(faults.Count, 0);

				foreach (var fault in faults)
				{
					Assert.True(!Path.IsPathRooted(fault.FaultPath), "Fault's FaultPath should be relative");

					var detail = db.GetFaultById(fault.Id);
					Assert.NotNull(detail);
					Assert.Greater(detail.Files.Count, 0);

					foreach (var file in detail.Files)
					{
						var fullPath = Path.Combine(job.LogPath, fault.FaultPath, file.FullName);
						Assert.True(File.Exists(fullPath), "File '{0}' should exist!".Fmt(fullPath));
					}
				}
			}
		}

		[Test]
		public void TestDirectoryExists1()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					using (var db = new NodeDatabase())
						job = db.GetJob(ctx.config.id);
				}

				var fault = "";

				if (!ctx.reproducingFault && ctx.currentIteration == 3)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3 Fault",
				"* C 3",
				"*   1",
				"* C 2",
				"*   2",
				"* C 3",
				"    3 Fault",
				"*   3",
				"* C 3",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath, false))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(2, faults.Count);

				Assert.AreEqual(false, faults[0].Reproducible);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "3C"), faults[0].FaultPath);
				Assert.AreEqual(false, faults[1].Reproducible);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "3C_1"), faults[1].FaultPath);
			}
		}

		[Test]
		public void TestDirectoryExists2()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					using (var db = new NodeDatabase())
						job = db.GetJob(ctx.config.id);
				}

				var fault = "";

				if ((!ctx.reproducingFault && ctx.currentIteration == 3 && !ctx.controlIteration) ||
					(ctx.reproducingFault && ctx.currentIteration == 3 && ctx.controlIteration && ctx.reproducingIterationJumpCount > 1) ||
					(!ctx.reproducingFault && ctx.currentIteration == 4))
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3",
				"    3 Fault",
				"*   3",
				"* C 3",
				"*   1",
				"* C 2",
				"*   2",
				"* C 3 Fault",
				"    4 Fault",
				"*   4",
				"* C 4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath, false))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(2, faults.Count);

				Assert.AreEqual(true, faults[0].Reproducible);
				Assert.AreEqual(IterationFlags.Control, faults[0].Flags);
				Assert.AreEqual(Path.Combine("Faults", "InjectedFault", "3C"), faults[0].FaultPath);
				Assert.AreEqual(false, faults[1].Reproducible);
				Assert.AreEqual(IterationFlags.Control, faults[1].Flags);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "4C"), faults[1].FaultPath);
			}
		}

		[Test]
		public void TestDirectoryExists3()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					// Create a dupplicate file on disk that will collide with
					// a file that the job logger will try and save
					using (var db = new NodeDatabase())
					{
						job = db.GetJob(ctx.config.id);
						var faultDir = Path.Combine(job.LogPath, "Faults", "InjectedFault", "3");
						Directory.CreateDirectory(faultDir);
						File.WriteAllText(Path.Combine(faultDir, "1.Initial.Action.bin"), "");
					}
				}

				var fault = "";

				if (ctx.currentIteration == 3 && !ctx.controlIteration)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3",
				"    3 Fault",
				"*   3 Fault",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath, false))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(1, faults.Count);

				var f = faults[0];
				Assert.AreEqual(true, f.Reproducible);
				Assert.AreEqual(Path.Combine("Faults", "InjectedFault", "3_1"), f.FaultPath);
			}
		}

		[Test]
		public void TestNoReproRecord()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				var fault = "";

				if (ctx.controlRecordingIteration)
				{
					// Create a dupplicate file on disk that will collide with
					// a file that the job logger will try and save
					using (var db = new NodeDatabase())
					{
						job = db.GetJob(ctx.config.id);
						var faultDir = Path.Combine(job.LogPath, "Faults", "InjectedFault", "3");
						Directory.CreateDirectory(faultDir);
						File.WriteAllText(Path.Combine(faultDir, "1.Initial.Action.bin"), "");
					}

					if (!ctx.reproducingFault)
					{
						ctx.faults.Add(new Fault
						{
							title = "InjectedFault",
							folderName = "InjectedFault",
							description = "InjectedFault",
							type = FaultType.Fault
						});

						fault = " Fault";
					}
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1 Fault",
				"* R 1",
				"    1",
				"    2",
				"  C 3",
				"    3",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath, false))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(1, faults.Count);

				var f = faults[0];
				Assert.AreEqual(false, f.Reproducible);
				Assert.AreEqual(IterationFlags.Record | IterationFlags.Control, f.Flags);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "1R"), f.FaultPath);
			}
		}
	}
}
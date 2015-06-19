using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Dapper;

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
	}
}
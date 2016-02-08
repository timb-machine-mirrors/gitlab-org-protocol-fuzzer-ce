using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Core.Test;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	[Peach]
	[Quick]
	class MetricsCacheTests
	{
		TempDirectory _tmp;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		[Test]
		public void TestFkConstraint1()
		{
			var job = new Job { LogPath = _tmp.Path };
			var cache = new AsyncDbCache(job);
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S1", 1);
			cache.ActionStarting("Action");
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.ActionStarting("A5");
			cache.StateStarting("S2", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.ActionStarting("A5");
			cache.StateStarting("S3", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.ActionStarting("A5");
			cache.StateStarting("S4", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S1", 1);
			cache.ActionStarting("Action");
			cache.ActionStarting("A1");
			cache.DataMutating("", "TheDataModel.Length", null, "SizedEdgeCase", "Data");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.ActionStarting("A5");
			cache.StateStarting("S2", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.ActionStarting("A5");
			cache.StateStarting("S3", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.DataMutating("P3", "TheDataModel.Data.Type", null, "StringUtf8BomLength", "Data");
			cache.ActionStarting("A5");
			cache.StateStarting("S4", 1);
			cache.ActionStarting("A1");
			cache.ActionStarting("A2");
			cache.ActionStarting("A3");
			cache.ActionStarting("A4");
			cache.ActionStarting("ACALL");
			cache.DataMutating("P2", "TheDataModel.Length", null, "NumberVariance", "Data");
			cache.DataMutating("P3", "TheDataModel.Length", null, "SizedDataEdgeCase", "Data");
			cache.IterationFinished();
			cache.TestFinished();
		}
	}
}

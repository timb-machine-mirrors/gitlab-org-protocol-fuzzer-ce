﻿using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	[Peach]
	[Quick]
	class MetricsCacheTests
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
		public void TestFKConstraint1()
		{
			//using (var db = new JobDatabase(_tmp.Path))
			//{
			//	var cache = new MetricsCache(db);
			//	cache.IterationStarting(1);
			//	cache.StateStarting("S1", 1);
			//	cache.ActionStarting("Action");
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S2", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S3", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S4", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.IterationStarting(1);
			//	cache.StateStarting("S1", 1);
			//	cache.ActionStarting("Action");
			//	cache.ActionStarting("A1");
			//	cache.DataMutating("", "TheDataModel.Length", "SizedEdgeCase", "Data");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S2", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S3", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.DataMutating("P3", "TheDataModel.Data.Type", "StringUtf8BomLength", "Data");
			//	cache.ActionStarting("A5");
			//	cache.StateStarting("S4", 1);
			//	cache.ActionStarting("A1");
			//	cache.ActionStarting("A2");
			//	cache.ActionStarting("A3");
			//	cache.ActionStarting("A4");
			//	cache.ActionStarting("ACALL");
			//	cache.DataMutating("P2", "TheDataModel.Length", "NumberVariance", "Data");
			//	cache.DataMutating("P3", "TheDataModel.Length", "SizedDataEdgeCase", "Data");
			//	cache.IterationFinished(db);
			//}
		}
	}
}

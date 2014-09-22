using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;
using System.Collections.Generic;

namespace Peach.Core.Test
{
	[TestFixture]
	class NumberEdgeCaseTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("NumberEdgeCase");

			Assert.False(runner.IsSupported(new Blob()));

			Assert.False(runner.IsSupported(new Number() { length = 7 }));
			Assert.False(runner.IsSupported(new Number() { length = 8 }));
			Assert.True(runner.IsSupported(new Number() { length = 9 }));
			Assert.True(runner.IsSupported(new Number() { length = 32 }));

			Assert.False(runner.IsSupported(new Flag() { length = 7 }));
			Assert.False(runner.IsSupported(new Flag() { length = 8 }));
			Assert.True(runner.IsSupported(new Flag() { length = 9 }));
			Assert.True(runner.IsSupported(new Flag() { length = 32 }));

			Assert.False(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("Hello") }));
			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("0") }));
			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("100") }));
			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("-100") }));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob");
			var dm = new DataModel("DM");

			dm.Add(blob);

			var m1 = runner.Sequential(blob);
			Assert.AreEqual(50, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01 }, dm.Value.ToArray());

			var m = runner.Sequential(blob);

			var len = 1;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(++len, val.Length);

				foreach (var b in val)
					Assert.AreEqual(0x01, b);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01 }, dm.Value.ToArray());

			var m = runner.Random(1000, blob);

			var lengths = new Dictionary<int, int>();

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				int cnt;
				lengths.TryGetValue(val.Length, out cnt);
				lengths[val.Length] = cnt + 1;

				foreach (var b in val)
					Assert.AreEqual(0x01, b);
			}

			Assert.True(!lengths.ContainsKey(1));
			Assert.True(lengths.ContainsKey(2));
			Assert.True(lengths.ContainsKey(50));

			Assert.Greater(lengths[2], lengths[50]);
		}
	}
}

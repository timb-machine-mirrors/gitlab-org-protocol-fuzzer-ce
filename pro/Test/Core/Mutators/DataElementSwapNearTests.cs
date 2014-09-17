using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;

namespace Peach.Core.Test
{
	[TestFixture]
	class DataElementSwapNearTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1");
			Assert.False(runner.IsSupported(blob1));

			var blob2 = new Blob("Blob2");
			Assert.False(runner.IsSupported(blob2));

			var dm = new DataModel("DM");
			dm.Add(blob1);
			dm.Add(blob2);

			Assert.False(runner.IsSupported(dm));
			Assert.True(runner.IsSupported(blob1));
			Assert.False(runner.IsSupported(blob2));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1");
			var blob2 = new Blob("Blob2");
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			var m1 = runner.Sequential(blob1);
			Assert.AreEqual(1, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var blob2 = new Blob("Blob2") { DefaultValue = new Variant(new byte[] { 0x02 }) };
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			Assert.AreEqual(new byte[] { 0x01, 0x02 }, dm.Value.ToArray());

			var m = runner.Sequential(blob1);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[] { 0x02, 0x01 }, val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var blob2 = new Blob("Blob2") { DefaultValue = new Variant(new byte[] { 0x02 }) };
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			Assert.AreEqual(new byte[] { 0x01, 0x02 }, dm.Value.ToArray());

			var m = runner.Random(10, blob1);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[] { 0x02, 0x01 }, val);
			}
		}
	}
}

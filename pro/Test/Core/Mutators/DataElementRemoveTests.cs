using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class DataElementRemoveTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob");
			Assert.False(runner.IsSupported(blob));

			var dm = new DataModel("DM");
			dm.Add(blob);

			Assert.False(runner.IsSupported(dm));
			Assert.True(runner.IsSupported(blob));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob");
			var dm = new DataModel("DM");

			dm.Add(blob);

			var m1 = runner.Sequential(blob);
			Assert.AreEqual(1, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01, 0x02, 0x03 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, dm.Value.ToArray());

			var m = runner.Sequential(blob);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[0], val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01, 0x02, 0x03 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, dm.Value.ToArray());

			var m = runner.Random(10, blob);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[0], val);
			}
		}

		[Test]
		public void TestArrayVariance()
		{
			Assert.Fail("Not implemented");

			// Ensure things work if we remove the only array item and
			// the array variance mutator runs on the array
		}
	}
}

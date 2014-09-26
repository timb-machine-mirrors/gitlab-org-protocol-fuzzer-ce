using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class BlobChangeRandomTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("BlobChangeRandom");

			Assert.True(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
			}));

			Assert.True(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(new byte[0]),
			}));

			Assert.False(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
				isMutable = false,
			}));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("BlobChangeRandom");

			var m1 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[1]) });
			Assert.AreEqual(1, m1.Count());

			var m2 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[50]) });
			Assert.AreEqual(50, m2.Count());

			var m3 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[500]) });
			Assert.AreEqual(100, m3.Count());

			var m4 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[0]) });
			Assert.AreEqual(0, m4.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("BlobChangeRandom");
			var src = new byte[10];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestSequentialOne()
		{
			var runner = new MutatorRunner("BlobChangeRandom");
			var src = new byte[1];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("BlobChangeRandom");
			var src = new byte[10];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomOne()
		{
			var runner = new MutatorRunner("BlobChangeRandom");
			var src = new byte[1];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			var numSame = 0;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				if (src.SequenceEqual(val))
					++numSame;

				Assert.AreEqual(src.Length, val.Length);
			}

			// It is possible for random to return the same single byte
			Assert.Less(numSame, 5);
		}

		[Test]
		public void TestRandomZero()
		{
			var runner = new MutatorRunner("BlobChangeRandom");
			var src = new byte[0];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src, val);
			}
		}
	}
}

using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class BlobExpandZeroTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("BlobExpandZero");

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
			var runner = new MutatorRunner("BlobExpandZero");

			var m1 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[1]) });
			Assert.AreEqual(255, m1.Count());

			var m2 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[50]) });
			Assert.AreEqual(255, m2.Count());

			var m3 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[500]) });
			Assert.AreEqual(255, m3.Count());

			var m4 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[0]) });
			Assert.AreEqual(255, m4.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("BlobExpandZero");
			var src = new byte[10];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			int len = 11;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(len++, val.Length);
			}
		}

		[Test]
		public void TestSequentialOne()
		{
			var runner = new MutatorRunner("BlobExpandZero");
			var src = new byte[1];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			int len = 2;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(len++, val.Length);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("BlobExpandZero");
			var src = new byte[10];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomOne()
		{
			var runner = new MutatorRunner("BlobExpandZero");
			var src = new byte[1];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomZero()
		{
			var runner = new MutatorRunner("BlobExpandZero");
			var src = new byte[0];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src, val);
			}
		}
	}
}

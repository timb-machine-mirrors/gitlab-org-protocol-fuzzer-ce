using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;
using System.Collections.Generic;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class NumberRandomTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("NumberRandom");

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
			var runner = new MutatorRunner("NumberRandom");
			var signed = new bool[] { false, true };

			foreach (var s in signed)
			{
				for (int len = 9; len <= 64; ++len)
				{
					var n = new Dom.Number("num") { length = len, Signed = s };

					var m = runner.Sequential(n);

					// Count should be sqrt of max space, capped at 5000
					var range = (ulong)((long)n.MaxValue - n.MinValue);
					var sqrt = (ulong)Math.Sqrt(range);
					var count = Math.Min(sqrt, 5000);

					Assert.AreEqual(count, m.Count());
				}
			}

			var str = new Dom.String("str") { DefaultValue = new Variant("1") };
			var cnt = runner.Sequential(str).Count();
			Assert.AreEqual(5000, cnt);
		}

		[Test]
		public void TestSequential()
		{
		}

		[Test]
		public void TestRandom()
		{
		}
	}
}

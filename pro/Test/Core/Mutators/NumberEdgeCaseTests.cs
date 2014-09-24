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

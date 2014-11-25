using System;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class DoubleRandomTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DoubleRandom");

			Assert.False(runner.IsSupported(new Blob()));

			Assert.True(runner.IsSupported(new Peach.Core.Dom.Double() { length = 64 }));
			Assert.True(runner.IsSupported(new Peach.Core.Dom.Double() { length = 32 }));

			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("0") }));
			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("100") }));
			Assert.True(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("-100") }));
			Assert.False(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("NaN") }));
			Assert.False(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("Infinity") }));
			Assert.False(runner.IsSupported(new Dom.String() { DefaultValue = new Variant("-Infinity") }));
		}

		[Test]
		public void Double64BitTest()
		{
			var runner = new MutatorRunner("DoubleRandom");

			var dble = new Dom.Double("Double") { DefaultValue = new Variant(1.0) };

			var m = runner.Random(500, dble);

			Assert.AreEqual(500, m.Count());
		}

		[Test]
		public void Double32BitTest()
		{
			var runner = new MutatorRunner("DoubleRandom");

			var dble = new Dom.Double("Double") { DefaultValue = new Variant(1.0), length = 32 };

			var m = runner.Random(500, dble);

			Assert.AreEqual(500, m.Count());
		}
	}
}

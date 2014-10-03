using System;
using System.Linq;
using Peach.Core;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class StringLengthVarianceTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringLengthVariance");

			var str = new Dom.String("String");

			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringLengthVariance");

			var str = new Dom.String("String");


			// Default length +/- 50 with a min of 0, not invluding default

			str.DefaultValue = new Variant("");
			var m1 = runner.Sequential(str);
			Assert.AreEqual(50, m1.Count());

			str.DefaultValue = new Variant("0");
			var m2 = runner.Sequential(str);
			Assert.AreEqual(51, m2.Count());

			str.DefaultValue = new Variant("01234");
			var m3 = runner.Sequential(str);
			Assert.AreEqual(55, m3.Count());

			str.DefaultValue = new Variant(new string('A', 300));
			var m4 = runner.Sequential(str);
			Assert.AreEqual(100, m4.Count());

			foreach (var item in m4)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Should not get default lenth back out
				Assert.AreNotEqual(asStr.Length, 300);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringLengthVariance");

			var str = new Dom.String("String");

			str.DefaultValue = new Variant(new string('A', 300));

			var m = runner.Random(1000, str);
			Assert.AreEqual(1000, m.Count());

			// Ensure all items are strings
			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}
		}
	}
}

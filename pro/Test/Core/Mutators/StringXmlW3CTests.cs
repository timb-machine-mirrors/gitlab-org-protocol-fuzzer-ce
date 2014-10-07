using System;
using System.Linq;
using Peach.Core;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class StringXmlW3CTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringXmlW3C");

			var str = new Dom.String("String");

			Assert.False(runner.IsSupported(str));

			str.Hints.Add("XML", new Dom.Hint("XML", "xml"));

			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringXmlW3C");

			var str = new Dom.String("String");
			str.Hints.Add("XML", new Dom.Hint("XML", "xml"));

			var m = runner.Sequential(str);
			Assert.AreEqual(1510, m.Count());

			foreach (var item in m)
			{
				var val = item.Value.ToArray();
				Assert.NotNull(val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringXmlW3C");

			var str = new Dom.String("String");
			str.Hints.Add("XML", new Dom.Hint("XML", "xml"));

			var m = runner.Random(2000, str);
			Assert.AreEqual(2000, m.Count());

			foreach (var item in m)
			{
				var val = item.Value.ToArray();
				Assert.NotNull(val);
			}
		}
	}
}

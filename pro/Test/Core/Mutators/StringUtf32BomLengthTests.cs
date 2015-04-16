using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringUtf32BomLengthTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf32BomLength");

			var str = new Peach.Core.Dom.String("String");
			str.stringType = StringType.utf16;

			Assert.True(runner.IsSupported(str));

			str.DefaultValue = new Variant("hello");
			Assert.True(runner.IsSupported(str));

			str.isMutable = false;
			Assert.False(runner.IsSupported(str));

			str.isMutable = true;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.ascii;
			Assert.False(runner.IsSupported(str));

			str.stringType = StringType.utf16;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf16be;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf32;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf7;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf8;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf32be;
			Assert.True(runner.IsSupported(str));

			str.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
			Assert.False(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringUtf32BomLength");

			var str = new Peach.Core.Dom.String("String");
			str.stringType = StringType.utf16;

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

			var tokenBE = new BitStream(Encoding.BigEndianUTF32.ByteOrderMark);
			var tokenLE = new BitStream(Encoding.UTF32.ByteOrderMark);

			var cntBE = 0;
			var cntLE = 0;
			var cntBoth = 0;

			foreach (var item in m4)
			{
				var bs = item.Value;

				bool hasBE = bs.IndexOf(tokenBE, 0) != -1;
				bool hasLE = bs.IndexOf(tokenLE, 0) != -1;

				if (hasBE && hasLE)
					++cntBoth;
				else if (hasBE)
					++cntBE;
				else if (hasLE)
					++cntLE;
				else
					Assert.Fail("Missing BOM in mutated string");
			}

			Assert.Greater(cntBE, 0);
			Assert.Greater(cntLE, 0);
			Assert.Greater(cntBoth, 0);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringUtf32BomLength");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };
			str.stringType = StringType.utf32;

			var m = runner.Random(500, str);

			Assert.AreEqual(500, m.Count());

			var tokenBE = new BitStream(Encoding.BigEndianUTF32.ByteOrderMark);
			var tokenLE = new BitStream(Encoding.UTF32.ByteOrderMark);

			var cntBE = 0;
			var cntLE = 0;
			var cntBoth = 0;

			foreach (var item in m)
			{
				var bs = item.Value;

				bool hasBE = bs.IndexOf(tokenBE, 0) != -1;
				bool hasLE = bs.IndexOf(tokenLE, 0) != -1;

				if (hasBE && hasLE)
					++cntBoth;
				else if (hasBE)
					++cntBE;
				else if (hasLE)
					++cntLE;
				else
					Assert.Fail("Missing BOM in mutated string");
			}

			Assert.Greater(cntBE, 0);
			Assert.Greater(cntLE, 0);
			Assert.Greater(cntBoth, 0);
		}
	}
}

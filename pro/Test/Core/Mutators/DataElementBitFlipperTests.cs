using System;
using System.Collections.Generic;
using System.Linq;

using Peach.Core.Dom;

using NUnit.Framework;

namespace Peach.Core.Test
{
	[TestFixture]
	class DataElementBitFlipperTests
	{
		class DummyTransformer : Transformer
		{
			public DummyTransformer(DataElement parent, Dictionary<string, Variant> args)
				: base(parent, args)
			{
			}

			protected override IO.BitwiseStream internalEncode(IO.BitwiseStream data)
			{
				throw new NotImplementedException();
			}

			protected override IO.BitStream internalDecode(IO.BitStream data)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var blob = new Dom.Blob("Blob");
			Assert.True(runner.IsSupported(blob));

			var str = new Dom.String("String");
			Assert.True(runner.IsSupported(str));

			var num = new Dom.Number("Number");
			Assert.True(runner.IsSupported(num));

			var flag = new Dom.Flag("Flag");
			Assert.True(runner.IsSupported(flag));

			var blk = new Dom.Block("Block");
			Assert.False(runner.IsSupported(blk));

			blk.transformer = new DummyTransformer(blk, null);
			Assert.True(runner.IsSupported(blk));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var str = new Dom.String("str") { DefaultValue = new Variant(new string('a', 100)) };
			Assert.AreEqual(800, runner.Sequential(str).Count());

			var num = new Dom.Number("num") { length = 32 };
			Assert.AreEqual(32, runner.Sequential(num).Count());

			var blob = new Dom.Blob("blob");
			Assert.AreEqual(0, runner.Sequential(blob).Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var src = Encoding.ASCII.GetBytes("Hello");
			var blob = new Blob("Blob") { DefaultValue = new Variant(src) };

			var m = runner.Sequential(blob);

			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				Assert.AreEqual(src.Length * 8, val.LengthBits);
				Assert.AreNotEqual(src, buf);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var src = Encoding.ASCII.GetBytes("Hello World");
			var blob = new Blob("Blob") { DefaultValue = new Variant(src) };

			var m = runner.Random(200, blob);

			var flipped = new byte[src.Length];

			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				Assert.AreEqual(src.Length * 8, val.LengthBits);
				Assert.AreNotEqual(src, buf);

				for (int j = 0; j < src.Length; j++)
				{
					// Record a '1' for each flipped bit
					flipped[j] |= (byte)(src[j] ^ buf[j]);
				}
			}

			// Every bit should have been flipped
			foreach (var b in flipped)
				Assert.AreEqual(0xff, b);
		}

		void TestTransformer(string transformer, bool lengthSame)
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block>
			<Block>
				<String name='str1' value='Hello' />
				<String name='str2' value='World' />
			</Block>

			<Number size='32' value='100' />
		</Block>
		<Transformer class='{0}' />
	</DataModel>
</Peach>
".Fmt(transformer);

			var dom = DataModelCollector.ParsePit(xml);

			var runner = new MutatorRunner("DataElementBitFlipper");

			Assert.True(runner.IsSupported(dom.dataModels[0]));

			var orig = dom.dataModels[0].Value.ToArray();
			var last = orig;

			var m = runner.Random(500, dom.dataModels[0]);

			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				if (lengthSame)
					Assert.AreEqual(orig.Length * 8, val.LengthBits);
				else
					Assert.AreNotEqual(last, buf);

				Assert.AreNotEqual(orig, buf);

				last = buf;
			}
		}

		[Test]
		public void TestNullTransformer()
		{
			TestTransformer("Null", true);
		}

		[Test]
		public void TestTransformer()
		{
			TestTransformer("Base64Encode", false);
		}
	}
}

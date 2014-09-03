using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peach.Core.Test
{
	[TestFixture]
	class EdgeCaseGeneratorTests
	{
		class Sample
		{
			public long min;
			public ulong max;
			public long[] edges;
		}

		[Test]
		public void TestEdges()
		{
			var samples = new Sample[]
			{
				new Sample()
				{
					min = 0,
					max = 1,
					edges = new long[]
					{
						0,
						1,
					}
				},
				new Sample()
				{
					min = 0,
					max = 127,
					edges = new long[]
					{
						0,
						127,
					}
				},
				new Sample()
				{
					min = -1,
					max = 2,
					edges = new long[]
					{
						-1,
						0,
						2,
					}
				},
				new Sample()
				{
					min = short.MinValue,
					max = ushort.MaxValue,
					edges = new long[]
					{
						short.MinValue,
						sbyte.MinValue,
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
					}
				},
				new Sample()
				{
					min = long.MinValue,
					max = long.MaxValue,
					edges = new long[]
					{
						long.MinValue,
						int.MinValue,
						short.MinValue,
						sbyte.MinValue,
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
						int.MaxValue,
						uint.MaxValue,
						long.MaxValue,
					}
				},
				new Sample()
				{
					min = 0,
					max = ulong.MaxValue,
					edges = new long[]
					{
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
						int.MaxValue,
						uint.MaxValue,
						long.MaxValue,
						-1, // ulong.MaxValue
					}
				},
			};

			foreach (var s in samples)
				Assert.AreEqual(s.edges, new EdgeCaseGenerator(s.min, s.max).Edges.ToArray());
		}

		[Test]
		public void TestRanges()
		{
			var e1 = new EdgeCaseGenerator(short.MinValue, (ulong)short.MaxValue);

			// 6 edges
			Assert.AreEqual(6, e1.Edges.Count);
			// Range 0: -32768 ====> 32640
			Assert.AreEqual(-128 - -32768, e1.Range(0));
			// Range 1: -128 ====> 128
			Assert.AreEqual(0 - -128, e1.Range(1));
			// Range 2: 0 ====> 127
			Assert.AreEqual(127 - 0, e1.Range(2));
			// Range 3: 127 ====> 127
			Assert.AreEqual(127 - 0, e1.Range(3));
			// Range 4: 255 ====> 128
			Assert.AreEqual(255 - 127, e1.Range(4));
			// Range 5: 32767 ====> 32512
			Assert.AreEqual(32767 - 255, e1.Range(5));
		}

		[Test]
		public void TestRangesUlong()
		{
			var e1 = new EdgeCaseGenerator(0, (ulong)ulong.MaxValue);

			// 9 edges
			Assert.AreEqual(9, e1.Edges.Count);
			// Range 0: 0
			Assert.AreEqual(0x7f - 0, e1.Range(0));
			// Range 1: 0x7f
			Assert.AreEqual(0x7f - 0, e1.Range(1));
			// Range 2: 0xff
			Assert.AreEqual(0xff - 0x7f, e1.Range(2));
			// Range 3: 0x7fff
			Assert.AreEqual(0x7fff - 0xff, e1.Range(3));
			// Range 4: 0xffff
			Assert.AreEqual(0xffff - 0x7fff, e1.Range(4));
			// Range 5: 0x7fffffff
			Assert.AreEqual(0x7fffffff - 0xffff, e1.Range(5));
			// Range 6: 0xffffffff
			Assert.AreEqual(0xffffffff - 0x7fffffff, e1.Range(6));
			// Range 7: 0x7fffffffffffffff
			Assert.AreEqual(0x7fffffffffffffff - 0xffffffff, e1.Range(7));
			// Range 8: 0xffffffffffffffff
			Assert.AreEqual(-1 - 0x7fffffffffffffff, e1.Range(8));

			// Ensure casts to ulong properly
			var asUlong = (ulong)0x8000000000000000;
			Assert.AreEqual(asUlong, unchecked((ulong)e1.Range(8)));
		}

		[Test]
		public void Random()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(sbyte.MinValue, (ulong)sbyte.MaxValue);

			bool gotMin = false;
			bool gotMax = false;
			bool gotZero = false;

			for (int i = 0; i < 1000; ++i)
			{
				var x = e.Next(rng);

				Assert.GreaterOrEqual(x, sbyte.MinValue);
				Assert.LessOrEqual(x, sbyte.MaxValue);

				gotMin |= x == sbyte.MinValue;
				gotMax |= x == sbyte.MaxValue;
				gotZero |= x == 0;
			}

			Assert.True(gotMin);
			Assert.True(gotMax);
			Assert.True(gotZero);

			// We expect calls to Next() to generate random numbers
			// that are outside of the valid range a couple of times.
			Assert.Greater(e.BadRandom, 500);
			Assert.Less(e.BadRandom, 1000);
		}

		[Test]
		public void IntRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(int.MinValue, (ulong)int.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 2000000; ++i)
			{
				var x = e.Next(rng);

				for (int j = 0; j < hits.Length; ++j)
					hits[j] |= x == e.Edges[j];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			Assert.AreEqual(0, e.BadRandom);
		}

		[Test]
		public void LongRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(long.MinValue, (ulong)long.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 500000; ++i)
			{
				var x = e.Next(rng);

				for (int j = 0; j < hits.Length; ++j)
					hits[j] |= x == e.Edges[j];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			Assert.AreEqual(0, e.BadRandom);
		}

		[Test]
		public void ULongRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(0, ulong.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 500000; ++i)
			{
				var x = (ulong)e.Next(rng);

				for (int j = 0; j < hits.Length - 1; ++j)
					hits[j] |= x < (ulong)e.Edges[j + 1];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length - 1; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", (ulong)e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			Assert.AreEqual(0, e.BadRandom);
		}
	}
}

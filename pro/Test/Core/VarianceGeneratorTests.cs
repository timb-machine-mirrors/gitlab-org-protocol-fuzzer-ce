using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Peach.Core.Test
{
	[TestFixture]
	class VarianceGeneratorTests
	{
		[Test]
		public void TestSbyte()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, sbyte.MinValue, sbyte.MaxValue);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, sbyte.MinValue);
				Assert.LessOrEqual(x, sbyte.MaxValue);

				++hits[x - sbyte.MinValue];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as -1 and 1
			double pctLhs = 1.0 * hits[128] / (hits[127] + hits[128]);
			double pctRhs = 1.0 * hits[128] / (hits[129] + hits[128]);

			Assert.LessOrEqual(pctLhs, 0.51);
			Assert.LessOrEqual(pctRhs, 0.51);
			Assert.Greater(pctLhs, 0.50);
			Assert.Greater(pctRhs, 0.50);
		}

		[Test]
		public void TestByteMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(byte.MinValue, byte.MinValue, byte.MaxValue);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, byte.MinValue);
				Assert.LessOrEqual(x, byte.MaxValue);

				++hits[x];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as 1
			double pct = 1.0 * hits[0] / (hits[0] + hits[1]);

			Assert.LessOrEqual(pct, 0.51);
			Assert.Greater(pct, 0.50);
		}

		[Test]
		public void TestByteMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(byte.MaxValue, byte.MinValue, byte.MaxValue);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, byte.MinValue);
				Assert.LessOrEqual(x, byte.MaxValue);

				++hits[x];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as 1
			double pct = 1.0 * hits[255] / (hits[254] + hits[255]);

			Assert.LessOrEqual(pct, 0.51);
			Assert.Greater(pct, 0.50);
		}

		[Test]
		public void TestLongZero()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, long.MinValue, long.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, -3 * short.MaxValue);
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestLongMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(long.MinValue + 255, long.MinValue, long.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, long.MinValue + (3 * short.MaxValue));
			}
		}

		[Test]
		public void TestLongMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(long.MaxValue - 255, long.MinValue, long.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, long.MaxValue - (3 * short.MaxValue));
			}
		}


		[Test]
		public void TestULongZero()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, ulong.MinValue, ulong.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestULongMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(255, ulong.MinValue, ulong.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestULongMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(ulong.MaxValue, ulong.MinValue, ulong.MaxValue);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, ulong.MaxValue - (3 * short.MaxValue));
			}
		}

		//[Test]
		public void MakeCsv()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(ulong.MaxValue - (4 * 4096), ulong.MinValue, ulong.MaxValue);

			var dict = new Dictionary<ulong, int>();

			for (long i = 0; i < 10000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				int cnt;
				dict.TryGetValue(x, out cnt);
				dict[x] = cnt + 1;
			}

			File.WriteAllLines("variance.csv", dict.Select(kv => "{0},{1}".Fmt(kv.Key + int.MaxValue, kv.Value)));
		}
	}
}

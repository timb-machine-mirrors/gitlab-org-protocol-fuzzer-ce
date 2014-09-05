using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Peach.Core
{
	/// <summary>
	/// Computes the edge cases of numbers inside a given space.
	/// </summary>
	public class EdgeCaseGenerator
	{
		// Don't need to add long.MinValue and ulong.MaxValue
		// to the edge cases since the algorithm will always
		// include 'min' and 'max' in the result
		static long[] edgeCases = new long[]
		{
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
		};

		long minValue;
		ulong maxValue;

		List<long> edges = new List<long>();
		List<ulong> range = new List<ulong>();
		List<ulong> stddev = new List<ulong>();
		List<double> weights = new List<double>();

		internal ulong BadRandom
		{
			get;
			private set;
		}

		/// <summary>
		/// Generates numbers around the edge cases that occur
		/// between a minimum and maximum value.
		/// </summary>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		public EdgeCaseGenerator(long min, ulong max)
		{
			if (min > 0)
				throw new ArgumentOutOfRangeException("min", "Parameter must be less than or equal to zero.");

			if (max == 0)
				throw new ArgumentOutOfRangeException("max", "Parameter must be greater than zero.");

			minValue = min;
			maxValue = max;

			// First edge case is 'min'
			edges.Add(minValue);

			edges.AddRange(edgeCases
				// Skip while the edge cases are too negative
				.SkipWhile(i => unchecked(i - min <= 0))
				// Take wile edge cases are < 0 but not too positive
				.TakeWhile(i => unchecked(i < 0 || max - (ulong)i - 1 < max)));

			// Last edge case is 'max'
			edges.Add(unchecked((long)maxValue));

			// Compute range and standard deviation
			for (int i = 0; i < edges.Count; ++i)
			{
				var edge = edges[i];

				ulong v;

				if (edge <= 0 && edge != unchecked((long)maxValue))
				{
					// If edge <= 0, it is the distance to next edge
					v = unchecked((ulong)(edges[i + 1] - edge));
				}
				else
				{
					// If edge > 0, it is the distance to the previous edge
					v = unchecked((ulong)(edge - edges[i - 1]));
				}

				range.Add(v);

				// We want the distribution to be a bell curve over
				// 2x the range.  This means stddev should be 2*range / 6
				// since 99% of the numbers will be 3 stddev away from the mean
				var s = v / 3;

				stddev.Add(s);

				Deviation += s;
			}

			// Weight each edge based on its range.
			ulong sum = 0;

			for (int i = 0; i < edges.Count; ++i)
			{
				var weight = 1.0 - (1.0 * sum / Deviation);

				System.Diagnostics.Debug.Assert(weight <= 1);
				System.Diagnostics.Debug.Assert(weight > 0);

				weights.Add(weight);
				sum += stddev[i];
			}

			// Set the minimum count to be a portion of the range
			// Set the count to be a portion of the range space of the generator
			Count = (int)Math.Sqrt(Deviation);
		}

		/// <summary>
		/// The list of edge cases in the number space.
		/// </summary>
		public IList<long> Edges
		{
			get
			{
				return edges.AsReadOnly();
			}
		}

		/// <summary>
		/// The sum of all the standard deviations at each edge case
		/// </summary>
		public ulong Deviation
		{
			get;
			private set;
		}

		/// <summary>
		/// The minimum number of random numbers that sohuld be generated.
		/// </summary>
		public int Count
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the range of numbers to consider for a given edge.
		/// If the edge is less than ore equal to zero, it is the distance to the next edge.
		/// If the edge is greater than zero, it is the distance from the previous edge.
		/// </summary>
		/// <remarks>
		/// If the edge is short.MinValue, the next edge will be sbyte.MinValue
		/// and the resultant range will be (sbyte.MinValue - short.MinValue).
		/// 
		/// If the edge is short.MaxValue, the previous edge will be byte.MaxValue
		/// and the resultant edge will be (short.MaxValue - byte.MaxValue).
		/// </remarks>
		/// <param name="edgeIndex">The edge index to computer the range for.</param>
		/// <returns>The range to generate values in.</returns>
		public ulong Range(int edgeIndex)
		{
			return range[edgeIndex];
		}

		/// <summary>
		/// Produce next edge case number around the given edge index.
		/// </summary>
		/// <param name="random">Random number generator to use.</param>
		/// <param name="edgeIndex">The edge index to pick a random number in.</param>
		/// <returns>A random number in the range for the given edge index.</returns>
		public long Next(Random random, int edgeIndex)
		{
			var edge = edges[edgeIndex];
			var sigma = stddev[edgeIndex];

			while (true)
			{
				var ret = (long)random.NextGaussian(edge, sigma);

				if (unchecked(maxValue - (ulong)ret < maxValue))
				{
					// Number is less than max as viewed as a ulong
					// Check minValue >= 0 first sinze ret can be massively negative for ulongs
					if (minValue >= 0 || minValue <= ret)
						return ret;
				}
				else if (ret <= 0 && minValue <= ret)
				{
					// Number is greater than max viewed as a ulong, so only
					// accept negative numbers that aren't too negative
					return ret;
				}

				CountBadRandom();
			}
		}

		/// <summary>
		/// Produce next edge case number for a randomly selected edge index.
		/// </summary>
		/// <param name="random">Random number generator to use.</param>
		/// <returns>A random number.</returns>
		public long Next(Random random)
		{
			var r = random.NextDouble();

			int i = weights.Count - 1;

			// Weights are [ 1.0, 0.5 ], r is 0.5, expect i == 0
			// Since weights[0] is always 1.0 and random never returns 1.0
			while (weights[i] <= r)
				--i;

			return Next(random, i);
		}

		[Conditional("DEBUG")]
		private void CountBadRandom()
		{
			++BadRandom;
		}
	}
}

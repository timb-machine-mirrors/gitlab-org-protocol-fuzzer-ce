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
		public long Range(int edgeIndex)
		{
			if (edgeIndex < 0)
				throw new ArgumentOutOfRangeException("edgeIndex", "Parameter must be greater than or equal to zero.");

			if (edgeIndex >= edges.Count)
				throw new ArgumentOutOfRangeException("edgeIndex", "Parameter must be smaller than one less than the number of edges.");

			var edge = edges[edgeIndex];

			if (edge <= 0 && edge != unchecked((long)maxValue))
			{
				// If edge <= 0, it is the distance to next edge
				return edges[edgeIndex + 1] - edge;
			}
			else
			{
				// If edge > 0, it is the distance to the previous edge
				return edge - edges[edgeIndex - 1];
			}
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
			var range = Range(edgeIndex);

			while (true)
			{
				var ret = (long)random.NextGaussian(edge, range / 3.0);

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
			var edgeIndex = random.Next(0, edges.Count);

			return Next(random, edgeIndex);
		}

		[Conditional("DEBUG")]
		private void CountBadRandom()
		{
			++BadRandom;
		}
	}
}

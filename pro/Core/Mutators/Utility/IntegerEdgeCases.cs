
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators.Utility
{
	/// <summary>
	/// Generate integer edge cases. The numbers produced are distributed 
	/// over a bell curve with the edge case as the center.
	/// </summary>
	public class IntegerEdgeCases
	{
		long[] longEdges;
		ulong[] ulongEdges;

		long minValue;
		ulong maxValue;

		bool isULong;

		public IntegerEdgeCases(long minValue, ulong maxValue)
		{
			isULong = maxValue > long.MaxValue;
			this.maxValue = maxValue;
			this.minValue = minValue;

			if(!isULong)
			{
				var edgeCases = new long[] { 
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
					long.MaxValue 
				};

				var edges = new List<long>();

				foreach(var l in edgeCases)
				{
					if (l >= minValue && l <= (long)maxValue)
					{
						edges.Add(l);
					}
				}

				longEdges = edges.ToArray();
			}
			else
			{
				ulong[] edgeCases = new ulong[] { 
					0, 
					(ulong)sbyte.MaxValue,
					byte.MaxValue, 
					(ulong)short.MaxValue, 
					ushort.MaxValue,
					int.MaxValue,
					uint.MaxValue, 
					long.MaxValue,
					ulong.MaxValue
				};

				var edges = new List<ulong>();

				foreach (ulong l in edgeCases)
				{
					if (l <= maxValue)
						edges.Add(l);
				}

				ulongEdges = edges.ToArray();
			}
		}

		/// <summary>
		/// Get the range of values to produce. The range is defined as the 
		/// distance between the prior edge to current edge again as much. For example,
		/// if the chosen edge case is 255, the range is 127->255+127.
		/// </summary>
		/// <param name="edgeIndex"></param>
		/// <returns></returns>
		public object Range(int edgeIndex)
		{
			if (isULong)
			{
				var minValue = edgeIndex > 0 ? ulongEdges[edgeIndex - 1] : ulong.MinValue;
				return (ulongEdges[edgeIndex] - minValue) * 2;
			}
			else
			{
				if (longEdges[edgeIndex] >= 0)
				{
					var minValue = edgeIndex > 0 ? longEdges[edgeIndex - 1] : long.MinValue;
					return Math.Abs((longEdges[edgeIndex] - minValue) * 2);
				}
				else
				{
					var minValue = longEdges[edgeIndex + 1];
					return Math.Abs((longEdges[edgeIndex] - minValue) * 2);
				}
			}
		}

		/// <summary>
		/// Produce next edge case number.
		/// </summary>
		/// <param name="random"></param>
		/// <returns></returns>
		public object Next(Peach.Core.Random random)
		{
			if (isULong)
			{
				var edgeIndex = random.Next(0, ulongEdges.Length);
				var edge = ulongEdges[edgeIndex];
				var range = (ulong) Range(edgeIndex);
				ulong ulValue = 0;

				do
				{
					ulValue = (ulong)(random.NextGaussian(edge, (range / 2) / 3));
				}
				while (ulValue > maxValue || ulValue < (ulong)minValue);

				return ulValue;
			}
			else
			{
				var edgeIndex = random.Next(0, longEdges.Length);
				var edge = longEdges[edgeIndex];
				var range = (long)Range(edgeIndex);
				long lValue = 0;

				do
				{
					lValue = (long)(random.NextGaussian(edge, (range / 2) / 3));
				}
				while (lValue > (long)maxValue || lValue < minValue);

				return lValue;
			}
		}
	}
}

// end
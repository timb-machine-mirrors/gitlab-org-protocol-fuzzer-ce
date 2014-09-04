//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Linq;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Generate integer edge cases. The numbers produced are distributed 
	/// over a bell curve with the edge case as the center.
	/// </summary>
	public abstract class IntegerEdgeCases : Mutator
	{
		Func<long> sequential;
		Func<long> random;
		int space;
		long min;
		ulong max;

		public IntegerEdgeCases(DataElement obj)
		{
			GetLimits(obj, out min, out max);

			var delta = unchecked((long)max - min);

			if (delta >= 0 && delta <= 0xff)
			{
				// We are <= a single byte, set the space size of the range
				space = (int)delta + 1;
				sequential = () => min + mutation;
				random = () => context.Random.Next(min, (long)max + 1);
			}
			else
			{
				// For more than a single byte, use edge case generator
				var gen = new EdgeCaseGenerator(min, max);

				// Random is same as sequential in this case
				sequential = random = () => gen.Next(context.Random);

				// Set the count to be a portion of the range space of the generator
				for (var i = 0; i < gen.Edges.Count; ++i)
					space += (int)Math.Sqrt(gen.Range(i));
			}
		}

		/// <summary>
		/// The logger to use.
		/// </summary>
		protected abstract NLog.Logger Logger
		{
			get;
		}

		/// <summary>
		/// Get the minimum and maximum values to generate edge cases for.
		/// </summary>
		/// <param name="obj">The element this mutator is bound to.</param>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		protected abstract void GetLimits(DataElement obj, out long min, out ulong max);

		/// <summary>
		/// Mutate the data element.
		/// </summary>
		/// <param name="obj">The element to mutate.</param>
		/// <param name="value">The value to use when mutating.</param>
		protected abstract void performMutation(DataElement obj, long value);

		/// <summary>
		/// Mutate the data element.  This is called when the value to be used
		/// for mutation is larger than long.MaxValue.
		/// </summary>
		/// <param name="obj">The element to mutate.</param>
		/// <param name="value">The value to use when mutating.</param>
		protected abstract void performMutation(DataElement obj, ulong value);

		public override uint mutation
		{
			get;
			set;
		}

		public override int count
		{
			get
			{
				return space;
			}
		}

		public override void sequentialMutation(DataElement obj)
		{
			doMutation(obj, sequential());
		}

		public override void randomMutation(DataElement obj)
		{
			doMutation(obj, random());
		}

		void doMutation(DataElement obj, long value)
		{
			// EdgeCaseGenerator gurantees value is between min/max
			// Promote to ulong if appropriate
			if (value < 0 && min >= 0)
			{
				var asUlong = unchecked((ulong)value);
				Logger.Trace("performMutation(value={0}", asUlong);
				performMutation(obj, asUlong);
			}
			else
			{
				Logger.Trace("performMutation(value={0}", value);
				performMutation(obj, value);
			}
		}
	}
}


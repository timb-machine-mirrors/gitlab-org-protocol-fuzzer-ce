using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators.Utility
{
	public abstract class IntegerVariance : Mutator
	{
		VarianceGenerator gen;

		bool signed;
		long value;
		long min;
		long max;

		public IntegerVariance(DataElement obj)
		{
			GetLimits(obj, out signed, out value, out min, out max);

			if (!signed)
				gen = new VarianceGenerator((ulong)value, (ulong)min, (ulong)max);
			else
				gen = new VarianceGenerator(value, min, max);
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
		/// <remarks>
		/// If value is unsigned, just cast it to a long.
		/// </remarks>
		/// <param name="obj">The element this mutator is bound to.</param>
		/// <param name="signed">Is the number space signed.</param>
		/// <param name="value">The value to center the variance distribution around.</param>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		protected abstract void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max);

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

		public sealed override uint mutation
		{
			get;
			set;
		}

		public sealed override int count
		{
			get
			{
				return gen.Count;
			}
		}

		public sealed override void sequentialMutation(DataElement obj)
		{
			// sequential is the same as random
			randomMutation(obj);
		}

		public sealed override void randomMutation(DataElement obj)
		{
			while (true)
			{
				var value = gen.Next(context.Random);

				// If we get our default value, pick again as that
				// is not really a mutation
				if (value == this.value)
					continue;

				// VarianceGenerator gurantees value is between min/max
				// Promote to ulong if appropriate
				if (value < 0 && !signed)
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

				return;
			}
		}
	}
}

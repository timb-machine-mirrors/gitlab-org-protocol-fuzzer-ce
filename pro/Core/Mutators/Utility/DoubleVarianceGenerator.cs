using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Peach.Core
{
	/// <summary>
	/// Generates a normal distribution of double precision numbers
	/// for a specified range centered around a specified value.
	/// The range of the distribution will be from [min,value] and [value,max].
	/// </summary>
	public class DoubleVarianceGenerator
	{
		double maxRange;

		double value;
		double min;
		double max;

		double weight;    // weight of the 'right' curve
		double sigmaLhs;  // sigma to use for the curve to the left hand side of value
		double sigmaRhs;  // sigma to use for the curve to the right hand side of value

		internal ulong BadRandom
		{
			get;
			private set;
		}

		public DoubleVarianceGenerator(double value, double min, double max, double maxRange)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException("min", "Parameter cannot be greater than max.");
			if (value > max)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be greater than max.");
			if (value < min)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be less than min.");

			this.maxRange = maxRange;

			Initialize(value, min, max);
		}

		public double Next(Random random)
		{
			var r = random.NextDouble();

			if (r < weight)
				// Ignore rhs 0 when a left curve exists
				return Next(random, sigmaRhs, sigmaLhs == 0);
			else
				// Ignore lhs 0 when right curve exists
				return Next(random, sigmaLhs, sigmaRhs == 0);
		}

		double Next(Random random, double sigma, bool floor)
		{
			while (true)
			{
				var num = random.NextGaussian();

				// Only want half a bell curve
				num = Math.Abs(num);

				double asLong = num * sigma;

				// If we are on the right side curve, make sure we don't
				// overflow max when shifting to be centered at value
				if (asLong > 0 && asLong > (max - value))
				{
					CountBadRandom();
					continue;
				}

				// If we are on the left  side curve, make sure we don't
				// overflow min when shifting to be centered at value
				if (asLong < 0 && -asLong > (value - min))
				{
					CountBadRandom();
					continue;
				}

				return value + asLong;
			}
		}

		void Initialize(double value, double min, double max)
		{
			this.value = value;
			this.min = min;
			this.max = max;

			// We want each side of value to be half a bell curve over
			// the range.  This means stddev should be 2 * range / 6
			// since 99% of the numbers will be 3 stddev away from the mean

			sigmaLhs = GetSigma(value, min);
			sigmaRhs = GetSigma(max, value);

			if (sigmaLhs == 0)
				weight = 1;
			else if (sigmaRhs == 0)
				weight = 0;
			else
				weight = 1.0 * sigmaRhs / (sigmaLhs + sigmaRhs);

			// Make left hand side negative
			sigmaLhs *= -1;
		}

		double GetSigma(double upper, double lower)
		{
			var ret = (upper - lower) / 3;

			ret = Math.Min(ret, maxRange);

			return ret;
		}

		void CountBadRandom()
		{
			++BadRandom;
		}
	}
}

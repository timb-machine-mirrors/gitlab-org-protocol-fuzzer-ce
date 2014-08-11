using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Enterprise;
using Peach.Core;

namespace Peach.Enterprise.Mutators.Utility
{
	public class IntegerVariance
	{
		long minValue;
		ulong maxValue;
		long lValue;
		ulong ulValue;
		bool signed;
		bool isUlong = false;

		public IntegerVariance(object value, long minValue, ulong maxValue, bool signed)
		{
			this.minValue = minValue;
			this.maxValue = maxValue;
			this.signed = signed;

			if (value is long)
				lValue = (long)value;
			else
			{
				ulValue = (ulong)value;
				isUlong = true;
			}
		}

		public object Next(Peach.Core.Random random)
		{
			if(isUlong)
			{
				ulong range = maxValue - (ulong)minValue;
				return (ulong) (random.NextGaussian() * (double)range);
			}
			else
			{
				long range = (long)maxValue - minValue;
				return (long) (random.NextGaussian() * (double)range);
			}
		}
	}
}

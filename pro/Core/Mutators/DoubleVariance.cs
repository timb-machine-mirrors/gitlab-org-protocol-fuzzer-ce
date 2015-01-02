using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	//Hide this mutator as its not fully tested
	[Mutator("DoubleVariance")]
	[Description("Produce random number in range of underlying element.")]
	public class DoubleVariance : Mutator
	{
		const int maxCount = 5000; // Maximum count is 5000

		DoubleVarianceGenerator gen;

		public DoubleVariance(DataElement obj)
			: base(obj)
		{

			var max = Math.Min((double)obj.InternalValue + 10, double.MaxValue);
			var min = Math.Max(-(double)obj.InternalValue - 10, double.MinValue);
			var maxRange = Math.Min(Math.Abs((double)obj.InternalValue) + 100, double.MaxValue / 3);

			var asDouble = obj as Peach.Core.Dom.Double;

			if (asDouble != null && obj.lengthAsBits == 32)
			{
				max = Math.Min((double)obj.InternalValue + 10, float.MaxValue);
				min = Math.Max(-(double)obj.InternalValue - 10, float.MinValue);
				maxRange = Math.Min(Math.Abs((double)obj.InternalValue) + 100, float.MaxValue / 3);

				if (maxRange > float.MaxValue)
					maxRange = float.MaxValue / 3;
			}

			if (max < min)
			{
				var tmp = max;
				max = min;
				min = tmp;
			}

			gen = new DoubleVarianceGenerator((double)obj.InternalValue, min, max, maxRange);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			var asDouble = obj as Peach.Core.Dom.Double;
			if (asDouble != null)
			{
				bool supported = true;

				supported = supported && !double.IsNaN((double)asDouble.DefaultValue);
				supported = supported && !double.IsInfinity((double)asDouble.DefaultValue);
				
				return obj.isMutable && supported;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return maxCount;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			obj.MutatedValue = new Variant(gen.Next(context.Random));
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

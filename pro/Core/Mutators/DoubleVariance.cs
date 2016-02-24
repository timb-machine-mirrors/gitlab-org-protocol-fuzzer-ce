using System;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Mutators.Utility;
using Double = Peach.Core.Dom.Double;
using String = Peach.Core.Dom.String;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Mutators
{
	//Hide this mutator as its not fully tested
	[Mutator("DoubleVariance")]
	[Description("Produce random number in range of underlying element.")]
	public class DoubleVariance : Mutator
	{
		const int MaxCount = 5000; // Maximum count is 5000

		readonly DoubleVarianceGenerator _gen;

		public DoubleVariance(DataElement obj)
			: base(obj)
		{
			var val = (double)obj.InternalValue;
			var abs = Math.Abs(val);

			double max;
			double min;
			double maxRange;

			var asDouble = obj as Double;

			if (asDouble != null && obj.lengthAsBits == 32)
			{
				max = Math.Min(abs + 10, float.MaxValue);
				min = Math.Max(-abs - 10, float.MinValue);
				maxRange = Math.Min(abs + 100, float.MaxValue / 3);
			}
			else
			{
				max = Math.Min(abs + 10, double.MaxValue);
				min = Math.Max(-abs - 10, double.MinValue);
				maxRange = Math.Min(abs + 100, double.MaxValue / 3);
			}

			_gen = new DoubleVarianceGenerator(val, min, max, maxRange);
		}

		// ReSharper disable once InconsistentNaming
		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			var asDouble = obj as Double;
			if (asDouble != null)
			{
				var supported = !double.IsNaN((double)asDouble.InternalValue);
				supported = supported && !double.IsInfinity((double)asDouble.InternalValue);
				
				return obj.isMutable && supported;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return MaxCount;
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
			obj.MutatedValue = new Variant(_gen.Next(context.Random));
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

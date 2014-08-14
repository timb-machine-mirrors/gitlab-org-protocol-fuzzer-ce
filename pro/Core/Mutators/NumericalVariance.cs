
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Core.Mutators.Utility;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("NumericalVariance")]
	[Description("Produce numbers that are defaultValue - N to defaultValue + N")]
	//[Hint("NumericalVariance-N", "Gets N by checking node for hint, or returns default (50).")]
	public class NumericalVarianceMutator : Mutator
	{
		long minValue;
		ulong maxValue;
		bool signed;

		IntegerVariance variance;

		public NumericalVarianceMutator(DataElement obj)
		{
			name = "NumericalVariance";

			if (obj is Peach.Core.Dom.String)
			{
				signed = true;
				minValue = Int64.MinValue;
				maxValue = UInt64.MaxValue;
			}
			else if (obj is Number)
			{
				signed = ((Number)obj).Signed;
				minValue = ((Number)obj).MinValue;
				maxValue = ((Number)obj).MaxValue;
			}
			else if (obj is Flag)
			{
				signed = false;
				minValue = 0;
				maxValue = UInt64.MaxValue; // TODO Get value from parent;
			}

			variance = new IntegerVariance(obj.Value, minValue, maxValue, signed);
		}

		public override uint mutation
		{
			// TODO - Make this work :)
			get { return 0; }
			set {  }
		}

		public override int count
		{
			// TODO - Make this work :)
			get { return 1000;  }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				if (obj.Hints.ContainsKey("NumericalString"))
					return true;

			// Ignore numbers <= 8 bits, they will be mutated with the
			// NumericalEdgeCaseMutator

			if (obj is Number && obj.isMutable)
				if (((Number)obj).lengthAsBits > 8)
					return true;

			if (obj is Flag && obj.isMutable)
				if (((Flag)obj).lengthAsBits > 8)
					return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String)
				obj.MutatedValue = new Variant(variance.Next(context.Random).ToString());
			else if (maxValue > long.MaxValue)
				obj.MutatedValue = new Variant((ulong)variance.Next(context.Random));
			else
				obj.MutatedValue = new Variant((long)variance.Next(context.Random));

			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

// end

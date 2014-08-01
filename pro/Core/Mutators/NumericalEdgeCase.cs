
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Enterprise.Mutators.Utility;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("NumericalEdgeCase")]
	[Description("Produce Gaussian distributed numbers around numerical edge cases.")]
	public class NumericalEdgeCase : Mutator
	{
		long minValue;
		ulong maxValue;
		bool signed;
		bool isULong;

		IntegerEdgeCases edgeCases;

		public NumericalEdgeCase(DataElement obj)
		{
			name = "NumericalEdgeCase";
			isULong = false;

			if (obj is Peach.Core.Dom.String)
			{
				signed = true;
				minValue = Int64.MinValue;
				maxValue = Int64.MaxValue;
			}
			else if (obj is Number || obj is Flag)
			{
				signed = ((Number)obj).Signed;
				minValue = ((Number)obj).MinValue;
				maxValue = ((Number)obj).MaxValue;

				if (((Number)obj).MaxValue > long.MaxValue)
					isULong = true;
			}

			edgeCases = new IntegerEdgeCases(minValue, maxValue);
		}

		public override uint mutation
		{
			// TODO - Make this work :)
			get { return 0; }
			set { }
		}

		public override int count
		{
			// TODO - Make this work :)
			get
			{ return 1000; }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
			{
				if (obj.Hints.ContainsKey("NumericalString"))
					return true;
			}

			if ((obj is Peach.Core.Dom.Number || obj is Peach.Core.Dom.Flag) && obj.isMutable)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			throw new NotSupportedException("TODO - Make this work");
		}

		public override void randomMutation(DataElement obj)
		{
			if(isULong)
				obj.MutatedValue = new Variant((ulong)edgeCases.Next(context.Random));
			else
				obj.MutatedValue = new Variant((long)edgeCases.Next(context.Random));

			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

// end

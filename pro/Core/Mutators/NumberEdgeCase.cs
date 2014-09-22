//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("NumberEdgeCase")]
	[Description("Produce Gaussian distributed numbers around numerical edge cases.")]
	public class NumberEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public NumberEdgeCase(DataElement obj)
			: base(obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected override void GetLimits(DataElement obj, out long min, out ulong max)
		{
			var asNum = obj as Dom.Number;
			if (asNum != null)
			{
				min = asNum.MinValue;
				max = asNum.MaxValue;
			}
			else
			{
				System.Diagnostics.Debug.Assert(obj is Dom.String);

				min = long.MinValue;
				max = long.MaxValue;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			// Ignore numbers <= 8 bits, they will be mutated
			// with the NumericalVariance mutator
			return obj is Dom.Number && obj.isMutable && obj.lengthAsBits > 8;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("StringLengthVariance")]
	[Description("Produce strings with lengths from length - N to length + N.")]
	public class StringLengthVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StringLengthVariance(DataElement obj)
			: base(obj, false)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var str = (string)obj.InternalValue;

			signed = false;
			min = 0;
			max = Utility.SizedHelpers.MaxExpansion(obj);
			value = Math.Min(str.Length, max);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			var limit = Utility.SizedHelpers.MaxExpansion(obj);
			if (value > limit)
			{
				logger.Trace("Skipping mutation, expansion by {0} would exceed max output size.", value);
				return;
			}

			// Same as edge case except for how value is picked
			StringLengthEdgeCase.Mutate(obj, value);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}

//
// Copyright (c) Deja vu Security
//

using System;
using System.Linq;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedVariance")]
	[Description("Change the length of sized data to count - N to count + N.")]
	public class SizedVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedVariance(DataElement obj)
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

		protected virtual bool OverrideRelation
		{
			get
			{
				return false;
			}
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			signed = false;
			value = (long)obj.InternalValue;
			min = 0;
			max = long.MaxValue;

			// If we are a number, make sure our max is not larger than max long
			// since stream lengths are tracked as longs
			var asNum = obj as Dom.Number;
			if (asNum != null)
				max = (long)Math.Min((ulong)max, asNum.MaxValue);
			else
				System.Diagnostics.Debug.Assert(obj is Dom.String);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Any mutable object with a size relation
			if (obj.isMutable && obj.relations.From<SizeRelation>().Any())
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			Utility.SizedHelpers.ExpandTo(obj, value, OverrideRelation);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}

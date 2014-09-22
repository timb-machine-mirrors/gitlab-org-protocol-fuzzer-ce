//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("NumberVariance")]
	public class NumberVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public NumberVariance(DataElement obj)
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

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var asNum = obj as Dom.Number;
			if (asNum != null)
			{
				signed = asNum.Signed;
				min = asNum.MinValue;
				max = (long)asNum.MaxValue;
				value = signed ? (long)asNum.DefaultValue : (long)(ulong)asNum.DefaultValue;
			}
			else
			{
				System.Diagnostics.Debug.Assert(obj is Dom.String);

				signed = true;
				min = long.MinValue;
				max = long.MaxValue;
				value = (long)obj.DefaultValue;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			return obj is Dom.Number && obj.isMutable;
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

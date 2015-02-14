//
// Copyright (c) Deja vu Security
//

using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("NumberVariance")]
	public class NumberVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public NumberVariance(DataElement obj)
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
			var asNum = obj as Peach.Core.Dom.Number;
			if (asNum != null)
			{
				signed = asNum.Signed;
				min = asNum.MinValue;
				max = (long)asNum.MaxValue;
				value = signed ? (long)asNum.DefaultValue : (long)(ulong)asNum.DefaultValue;
			}
			else
			{
				System.Diagnostics.Debug.Assert(obj is Peach.Core.Dom.String);

				signed = true;
				min = long.MinValue;
				max = long.MaxValue;
				value = (long)obj.DefaultValue;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			return obj is Peach.Core.Dom.Number && obj.isMutable;
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

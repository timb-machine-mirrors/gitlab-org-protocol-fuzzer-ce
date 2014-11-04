//
// Copyright (c) Deja vu Security
//

using System;
using System.Text;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("StringLengthEdgeCase")]
	[Description("Produce Gaussian distributed string lengths around numerical edge cases.")]
	public class StringLengthEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StringLengthEdgeCase(DataElement obj)
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
			min = 0;
			max = Math.Min(ushort.MaxValue, (ulong)Utility.SizedHelpers.MaxExpansion(obj));
			//max = ((ulong)Utility.SizedHelpers.MaxExpansion(obj));
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
				logger.Info("Skipping mutation, expansion by {0} would exceed max output size.", value);
				return;
			}

			Mutate(obj, value);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}

		internal static void Mutate(DataElement obj, long value)
		{
			var src = (string)obj.InternalValue;
			var dst = ExpandTo(src, value);

			obj.MutatedValue = new Variant(dst);
			obj.mutationFlags = MutateOverride.Default;
		}

		private static string ExpandTo(string value, long length)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new string('A', (int)length);
			}
			else if (value.Length >= length)
			{
				return value.Substring(0, (int)length);
			}

			var sb = new StringBuilder();

			while (sb.Length + value.Length < length)
				sb.Append(value);

			sb.Append(value.Substring(0, (int)(length - sb.Length)));

			return sb.ToString();
		}
	}
}

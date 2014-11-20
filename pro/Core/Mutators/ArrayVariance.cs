//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayVariance")]
	[Description("Change the length of arrays to count - N to count + N")]
	public class ArrayVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ArrayVariance(DataElement obj)
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
			var asSeq = (Dom.Sequence)obj;

			signed = false;
			min = 0;
            max = Utility.SizedHelpers.MaxDuplication(TargetElement(asSeq));
			value = Math.Min(asSeq.Count, max);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
            if (obj is Dom.Sequence && obj.isMutable && TargetElement(obj as Dom.Sequence) != null)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long num)
		{
            var objAsSeq = (Dom.Sequence)obj;

            var targetElem = TargetElement(objAsSeq);
            if (targetElem == null)
            {
                logger.Trace("Skipping mutation, the sequence currently has no elements.");
                return;
            }

			if (num > 0)
			{
                var limit = Utility.SizedHelpers.MaxDuplication(targetElem);

				if (num > limit)
				{
					logger.Trace("Skipping mutation, duplication by {0} would exceed max output size.", num);
					return;
				}
			}

			if (num < objAsSeq.Count)
			{
				// remove some items
				for (int i = objAsSeq.Count - 1; i >= num; --i)
				{
					if (objAsSeq[i] == null)
						break;

					objAsSeq.RemoveAt(i);
				}
			}
			else if (num > objAsSeq.Count)
			{
				// add some items, but do it by replicating
				// the last item over and over to save memory
				objAsSeq.CountOverride = (int)num;
			}
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}

        static DataElement TargetElement(Dom.Sequence asSeq)
        {
            if (asSeq.Count > 0)
                return asSeq[asSeq.Count - 1];

            var asArray = asSeq as Dom.Array;
            if (asArray != null)
                return ((Dom.Array)asSeq).OriginalElement;

            return null;
        }
	}
}

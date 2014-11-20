//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayEdgeCase")]
	[Description("Change the length of arrays to integer edge cases")]
	public class ArrayEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ArrayEdgeCase(DataElement obj)
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
			var asSeq = (Dom.Sequence)obj;
			min = ushort.MinValue;
            max = (ulong)Utility.SizedHelpers.MaxDuplication(TargetElement(asSeq));
		}

		public new static bool supportedDataElement(DataElement obj)
		{
            if (obj is Dom.Sequence && obj.isMutable && TargetElement(obj as Dom.Sequence) != null)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long num)
		{
            var objasSeq = (Dom.Sequence)obj;

            var targetElem = TargetElement(objasSeq);
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

			if (num < objasSeq.Count)
			{
				// remove some items
				for (int i = objasSeq.Count - 1; i >= num; --i)
				{
					if (objasSeq[i] == null)
						break;

					objasSeq.RemoveAt(i);
				}
			}
			else if (num > objasSeq.Count)
			{
				// add some items, but do it by replicating
				// the last item over and over to save memory
				// find random spot and replicate that item over and over
				objasSeq.CountOverride = (int)num;
				//objasSeq.ExpandTo((int)num);
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
                return ((Dom.Array)asArray).OriginalElement;

            return null;
		}
	}
}

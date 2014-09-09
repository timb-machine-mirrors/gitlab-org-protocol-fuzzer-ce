
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
			signed = false;
			value = ((Core.Dom.Array)obj).Count;
			min = uint.MinValue;
			max = uint.MaxValue;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.Array && obj.isMutable)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long num)
		{
			var objAsArray = (Core.Dom.Array)obj;

			if (num < objAsArray.Count)
			{
				// remove some items
				for (int i = objAsArray.Count - 1; i >= num; --i)
				{
					if (objAsArray[i] == null)
						break;

					objAsArray.RemoveAt(i);
				}
			}
			else if (num > objAsArray.Count)
			{
				// add some items, but do it by replicating
				// the last item over and over to save memory
				objAsArray.ExpandTo((int)num);
			}
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}

// end

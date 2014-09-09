//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayEdgeCases")]
	[Description("Change the length of arrays to integer edge cases")]
	public class ArrayEdgeCases : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public ArrayEdgeCases(DataElement obj)
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
			var objAsArray = (Dom.Array)obj;

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


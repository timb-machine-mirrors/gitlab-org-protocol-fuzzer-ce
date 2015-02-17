//
// Copyright (c) Deja vu Security
//

using System;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Mutators
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
			var asArray = (Peach.Core.Dom.Array)obj;
			min = ushort.MinValue;
			max = (ulong)Utility.SizedHelpers.MaxDuplication(LastElement(asArray));
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.Array && obj.isMutable)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long num)
		{
			var objAsArray = (Peach.Core.Dom.Array)obj;

			if (num > 0)
			{
				var limit = Utility.SizedHelpers.MaxDuplication(LastElement(objAsArray));

				if (num > limit)
				{
					logger.Trace("Skipping mutation, duplication by {0} would exceed max output size.", num);
					return;
				}
			}

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
				// find random spot and replicate that item over and over
				objAsArray.CountOverride = (int)num;
				//objAsArray.ExpandTo((int)num);
			}
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}

		static DataElement LastElement(Peach.Core.Dom.Array asArray)
		{
			if (asArray.Count == 0)
				return asArray.OriginalElement;

			return asArray[asArray.Count - 1];
		}
	}
}

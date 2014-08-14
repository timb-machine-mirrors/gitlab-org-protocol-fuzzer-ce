
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Core.Mutators.Utility;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Enterprise.Mutators
{
	[Mutator("ArrayVarianceMutator")]
	[Description("Change the length of arrays to count - N to count + N")]
	//[Hint("ArrayVarianceMutator-N", "Gets N by checking node for hint, or returns default (50).")]
	public class ArrayVariance : Mutator
	{
		int n;
		int minCount;
		int maxCount;
		int currentCount;
		int arrayCount;

		IntegerVariance variance;

		public ArrayVariance(DataElement obj)
		{
			name = "ArrayVariance";
			n = getN(obj, 50);
			arrayCount = ((Peach.Core.Dom.Array)obj).Count;
			minCount = arrayCount - n;
			maxCount = arrayCount + n;

			if (minCount < 0)
				minCount = 0;

			currentCount = minCount;

			variance = new IntegerVariance(arrayCount, ushort.MinValue, ushort.MaxValue, false);
		}

		protected int MinCount
		{
			get { return minCount; }
			set { minCount = value; }
		}

		protected int CurrentCount
		{
			get { return currentCount; }
			set { currentCount = value; }
		}

		public int getN(DataElement obj, int n)
		{
			// check for hint
			if (obj.Hints.ContainsKey(name + "-N"))
			{
				Hint h = null;
				if (obj.Hints.TryGetValue(name + "-N", out h))
				{
					try
					{
						n = Int32.Parse(h.Value);
					}
					catch (Exception ex)
					{
						throw new PeachException("Expected numerical value for Hint named " + h.Name, ex);
					}
				}
			}

			return n;
		}

		public override uint mutation
		{
			get { return (uint)(currentCount - minCount); }
			set { currentCount = (int)value + minCount; }
		}

		public override int count
		{
			get { return maxCount - minCount + 1; }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.Array && obj.isMutable)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			throw new NotSupportedException("TODO - Make this work");
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation(obj, (int)variance.Next(context.Random));
			obj.mutationFlags = MutateOverride.Default;
		}

		public void performMutation(DataElement obj, int num)
		{
			Core.Dom.Array objAsArray = (Core.Dom.Array)obj;

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
				objAsArray.ExpandTo(num);
			}
		}

	}
}

// end

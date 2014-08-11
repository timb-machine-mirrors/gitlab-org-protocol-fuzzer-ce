
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Enterprise.Mutators.Utility;
using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayEdgeCases")]
	[Description("Change the length of arrays to integer edge cases")]
	//[Hint("ArrayNumericalEdgeCases-N", "Gets N by checking node for hint, or returns default (50).")]
	public class ArrayEdgeCases : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		IntegerEdgeCases edgeCases;

		public ArrayEdgeCases(DataElement obj)
		{
			name = "ArrayEdgeCases";
			edgeCases = new IntegerEdgeCases(UInt16.MinValue, UInt16.MaxValue);
		}

		public override uint mutation
		{
			/* TODO - Make this work :) */
			get { return 0; }
			set {  }
		}

		public override int count
		{
			// TODO- Make this real :)
			get { return 1000; }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.Array && obj.isMutable)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			throw new NotSupportedException("TODO - Make this work!");
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation(obj, (int) edgeCases.Next(context.Random));
			obj.mutationFlags = MutateOverride.Default;
		}

		public void performMutation(DataElement obj, int num)
		{
			logger.Trace("performMutation(num=" + num + ")");

			Dom.Array objAsArray = (Dom.Array)obj;

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

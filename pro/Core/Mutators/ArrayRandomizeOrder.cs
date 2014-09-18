//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayRandomizeOrder")]
	[Description("Randomize the order of the array")]
	public class ArrayRandomizeOrder : Mutator
	{
		public ArrayRandomizeOrder(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asArray = obj as Dom.Array;

			if (asArray != null && asArray.isMutable && asArray.Count > 1)
				return true;

			return false;
		}

		public override int count
		{
			get
			{
				// TODO: Make this reasonable
				// Should probably be array count factorial capped at a reasonable limit
				return 50;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation((Dom.Array)obj);
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation((Dom.Array)obj);
		}

		void performMutation(Dom.Array obj)
		{
			obj.mutationFlags = MutateOverride.Default;

			try
			{
				// Defer all updates
				obj.BeginUpdate();

				// Fisher-Yates shuffle directly on the array
				int n = obj.Count;
				int k = 0;

				while (n > 1)
				{
					k = context.Random.Next(0, n);
					n--;

					obj.SwapElements(k, n);
				}
			}
			finally
			{
				// Invalidate at the end
				obj.EndUpdate();
			}
		}
	}
}

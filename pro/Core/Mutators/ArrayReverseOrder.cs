//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("ArrayReverseOrder")]
	[Description("Reverse the order of the array")]
	public class ArrayReverseOrder : Mutator
	{
		public ArrayReverseOrder(DataElement obj)
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
				return 1;
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

				int i = obj.Count - 1;
				int j = 0;

				while (i > j)
				{
					obj.SwapElements(i--, j++);
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


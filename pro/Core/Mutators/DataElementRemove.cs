//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Generates a single test cases by removing a data element.
	/// </summary>
	[Mutator("DataElementRemove")]
	[Description("Removes an element from the data model")]
	public class DataElementRemove : Mutator
	{
		public DataElementRemove(DataElement obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj.isMutable && obj.parent != null && !(obj is Flag))
				return true;

			return false;
		}

		public override int count
		{
			get { return 1; }
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			obj.parent.Remove(obj);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

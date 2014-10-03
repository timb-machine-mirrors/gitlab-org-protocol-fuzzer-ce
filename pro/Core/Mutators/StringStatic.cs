//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringStatic")]
	[Description("Perform common string mutations")]
	public partial class StringStatic : Mutator
	{
		public StringStatic(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return true;

			return false;
		}

		public override int count
		{
			get
			{
				return values.Length;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			obj.mutationFlags = MutateOverride.Default;
			obj.MutatedValue = new Variant(values[mutation]);
		}

		public override void randomMutation(DataElement obj)
		{
			var idx = context.Random.Next(0, values.Length);

			obj.mutationFlags = MutateOverride.Default;
			obj.MutatedValue = new Variant(values[idx]);
		}
	}
}

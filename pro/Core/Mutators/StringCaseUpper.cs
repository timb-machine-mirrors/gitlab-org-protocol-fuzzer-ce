//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringCaseUpper")]
	[Description("Change the string to be all uppercase.")]
	public class StringCaseUpper : Mutator
	{
		public StringCaseUpper(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
			{
				// Esure the string changes when changing the case.
				// TODO: Investigate if it is faster to go 1 char at a time.
				var str = (string)obj.InternalValue;

				if (str != str.ToUpper())
					return true;
			}

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
			var str = (string)obj.InternalValue;

			obj.MutatedValue = new Variant(str.ToUpper());
			obj.mutationFlags = MutateOverride.Default;
		}

		public override void randomMutation(DataElement obj)
		{
			sequentialMutation(obj);
		}
	}
}

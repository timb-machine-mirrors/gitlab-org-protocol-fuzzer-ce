//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringRandom")]
	[Description("Produce random ascii strings of random lengths.")]
	public class StringRandom : Utility.StringMutator
	{
		public StringRandom(DataElement obj)
			: base(obj, 0x00, 0x7f)
		{
		}

		protected override int GetMutatedLength(DataElement obj)
		{
			// TODO: Is there something nicer we could do for
			// coming up with the length of the mutated string?
			return context.Random.Next(0, ushort.MaxValue + 1);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Override so we attach to all strings, not just unicode
			if (obj is Dom.String && obj.isMutable)
				return true;

			return false;
		}
	}
}

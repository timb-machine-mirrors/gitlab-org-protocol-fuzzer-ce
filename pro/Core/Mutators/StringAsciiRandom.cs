//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringAsciiRandom")]
	[Description("Produce random strings using the ascii character set.")]
	public class StringAsciiRandom : Utility.StringMutator
	{
		public StringAsciiRandom(DataElement obj)
			: base(obj, 0x00, 0x7f)
		{
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

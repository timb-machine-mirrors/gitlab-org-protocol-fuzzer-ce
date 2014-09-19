//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	[Mutator("StringUnicodeAbstractCharacters")]
	[Description("Produce string comprised of unicode abstract characters.")]
	public class StringUnicodeAbstractCharacters : Utility.StringMutator
	{
		// TODO: Populate this with something
		static readonly int[] codePoints = new int[0];

		public StringUnicodeAbstractCharacters(DataElement obj)
			: base(obj, codePoints)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// TODO: Remove this override once codePoints is populated
			return false;
		}
	}
}

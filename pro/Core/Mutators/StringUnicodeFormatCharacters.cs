//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Format characters are characters that do not have a visible appearance,
	/// but may have an effect on the appearance or behavior of neighboring characters.
	/// For example, U+200C ZERO WIDTH NON-JOINER and U+200D ZERO WIDTH JOINER may
	/// be used to change the default shaping behavior of adjacent characters
	/// (e.g. to inhibit ligatures or request ligature formation).
	/// There are 152 format characters in Unicode 7.0.
	/// </summary>
	[Mutator("StringUnicodeFormatCharacters")]
	[Description("Produce string comprised of unicode format characters.")]
	public class StringUnicodeFormatCharacters : Utility.StringMutator
	{
		// TODO: Populate this with something
		static readonly int[] codePoints = new int[0];

		public StringUnicodeFormatCharacters(DataElement obj)
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

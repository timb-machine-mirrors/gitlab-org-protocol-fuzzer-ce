//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Private Use Area: U+E000..U+F8FF (6,400 characters)
	/// </summary>
	[Mutator("StringUnicodePrivateUseArea")]
	[Description("Produce a random string from the Unicode private use area character set.")]
	public class StringUnicodePrivateUseArea : Utility.StringMutator
	{
		public StringUnicodePrivateUseArea(DataElement obj)
			: base(obj, 0xE000, 0xF8FF)
		{
		}
	}
}

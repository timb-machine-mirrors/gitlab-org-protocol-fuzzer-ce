//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 0 (0 - 0xffff).
	/// </summary>
	[Mutator("StringUnicodePlane0")]
	[Description("Produce a random string from the Unicode Plane 0 character set.")]
	public class StringUnicodePlane0 : Utility.StringMutator
	{
		public StringUnicodePlane0(DataElement obj)
			: base(obj, 0x0000, 0xFFFF)
		{
		}
	}
}

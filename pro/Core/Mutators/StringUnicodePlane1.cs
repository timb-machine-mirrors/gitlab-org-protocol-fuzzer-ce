//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 1 (0x10000 - 0x1ffff).
	/// </summary>
	[Mutator("StringUnicodePlane1")]
	[Description("Produce a random string from the Unicode Plane 1 character set.")]
	public class StringUnicodePlane1 : Utility.StringMutator
	{
		public StringUnicodePlane1(DataElement obj)
			: base(obj, 0x10000, 0x1FFFF)
		{
		}
	}
}

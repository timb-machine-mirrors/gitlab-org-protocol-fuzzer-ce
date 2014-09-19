//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 2 (0x20000 - 0x2ffff).
	/// </summary>
	[Mutator("StringUnicodePlane2")]
	[Description("Produce a random string from the Unicode Plane 2 character set.")]
	public class StringUnicodePlane2 : Utility.StringMutator
	{
		public StringUnicodePlane2(DataElement obj)
			: base(obj, 0x20000, 0x2FFFF)
		{
		}
	}
}

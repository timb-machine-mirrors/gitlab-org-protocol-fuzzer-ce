//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 14 (0xe0000 - 0xefffd).
	/// </summary>
	[Mutator("StringUnicodePlane14")]
	[Description("Produce a random string from the Unicode Plane 14 character set.")]
	public class StringUnicodePlane14 : Utility.StringMutator
	{
		public StringUnicodePlane14(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0xE0000, 0xEFFFD + 1);
		}
	}
}

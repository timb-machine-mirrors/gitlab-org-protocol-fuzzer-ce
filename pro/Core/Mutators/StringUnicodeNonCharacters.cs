using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peach.Enterprise.Mutators
{
	/// <summary>
	/// Certain noncharacter code points are guaranteed never to be used for encoding characters, although applications may make use of these code points internally if they wish. There are sixty-six noncharacters: U+FDD0..U+FDEF and any code point ending in the value FFFE or FFFF (i.e. U+FFFE, U+FFFF, U+1FFFE, U+1FFFF, ... U+10FFFE, U+10FFFF). The set of noncharacters is stable, and no new noncharacters will ever be defined.[14]
	/// </summary>
	class StringUnicodeNonCharacters
	{
	}
}

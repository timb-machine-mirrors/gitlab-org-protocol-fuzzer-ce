﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Fixup used in testing.  Will copy another elements value into us.")]
	[Fixup("CopyValue", true)]
	[Fixup("CopyValueFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Serializable]
	public class CopyValueFixup : Fixup
	{
		public CopyValueFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];

			// Use InternalValue so type information is preserved
			return elem.InternalValue;
		}
	}
}

// end

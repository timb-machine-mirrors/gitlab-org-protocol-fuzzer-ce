
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
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
	[Description("Will fill a BitStream with incrementing values from 'start' to 'stop'.")]
	[Fixup("FillValue", true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("start", typeof(byte), "Inclusive start fill value")]
	[Parameter("stop", typeof(byte), "Inclusive stop fill value")]
	[Serializable]
	public class FillValueFixup : Fixup
	{
		// Needed for ParameterParser to work
		public byte start { get; protected set; }
		public byte stop { get; protected set; }
		public DataElement _ref { get; protected set; }

		public FillValueFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);
			if (stop < start)
				throw new PeachException("Value of 'start' must be less than or equal to the value of 'stop'.");
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];
			var val = elem.Value;
			BitStream bs = new BitStream();

			int cycle = stop - start + 1;

			for (int i = 0; i < val.Length; ++i)
				bs.WriteByte((byte)((i % cycle) + start));

			bs.SeekBits(0, System.IO.SeekOrigin.Begin);
			bs.SetLengthBits(val.LengthBits);

			return new Variant(bs);
		}
	}
}

// end

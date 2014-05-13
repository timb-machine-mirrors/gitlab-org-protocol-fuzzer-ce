
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
//   Jordyn Puryear (jordyn@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Peach.Core.Dom;
using Peach.Core.Fixups;
using Peach.Core.IO;
using Peach.Core;

namespace Peach.Enterprise.Fixups
{
	[Description("Secure Random Number Fixup.")]
	[Fixup("SecureRandomNumber", true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("Length", typeof(int), "Length in bytes to return")]
	[Serializable]
	public class SecureRandomNumberFixup : VolatileFixup
	{
		static void Parse(string str, out DataElement val)
		{
			val = null;
		}

		public int Length { get; set; }
		protected DataElement _ref { get; set; }

		public SecureRandomNumberFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);

			if (Length <= 0)
				throw new PeachException("The length must be greater than 0.");
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			if (elements["ref"].hasLength && Length > elements["ref"].length)
				throw new PeachException("Length is greater than 'ref' elements size.");

			var buf = new byte[Length];
			var rng = new RNGCryptoServiceProvider();

			rng.GetBytes(buf);
			
			return new Variant(new BitStream(buf));
		}
	}
}

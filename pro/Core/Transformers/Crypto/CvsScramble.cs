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

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("CVS pserver password scramble.")]
	[Transformer("CvsScramble", true)]
	[Transformer("crypto.CvsScramble")]
	[Serializable]
	public class CvsScramble : Transformer
	{
		byte[] shifts = new byte[] 
		{
			  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15,
			 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
			114,120, 53, 79, 96,109, 72,108, 70, 64, 76, 67,116, 74, 68, 87,
			111, 52, 75,119, 49, 34, 82, 81, 95, 65,112, 86,118,110,122,105,
			 41, 57, 83, 43, 46,102, 40, 89, 38,103, 45, 50, 42,123, 91, 35,
			125, 55, 54, 66,124,126, 59, 47, 92, 71,115, 78, 88,107,106, 56,
			 36,121,117,104,101,100, 69, 73, 99, 63, 94, 93, 39, 37, 61, 48,
			 58,113, 32, 90, 44, 98, 60, 51, 33, 97, 62, 77, 84, 80, 85,223,
			225,216,187,166,229,189,222,188,141,249,148,200,184,136,248,190,
			199,170,181,204,138,232,218,183,255,234,220,247,213,203,226,193,
			174,172,228,252,217,201,131,230,197,211,145,238,161,179,160,212,
			207,221,254,173,202,146,224,151,140,196,205,130,135,133,143,246,
			192,159,244,239,185,168,215,144,139,165,180,157,147,186,214,176,
			227,231,219,169,175,156,206,198,129,164,150,210,154,177,134,127,
			182,128,158,208,162,132,167,209,149,241,153,251,237,236,171,195,
			243,233,253,240,194,250,191,155,142,137,245,235,163,242,178,152
		};

		byte marker = 0x41; // 'A'

		public CvsScramble(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			BitStream ret = new BitStream();
			ret.WriteByte(marker);

			int value;
			while ((value = data.ReadByte()) != -1)
				ret.WriteByte(shifts[value]);

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			int value = data.ReadByte();
			if (value != marker)
				throw new SoftException("Unknown scrambling method.");

			BitStream ret = new BitStream();
			while ((value = data.ReadByte()) != -1)
				ret.WriteByte(shifts[value]);

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}

// end

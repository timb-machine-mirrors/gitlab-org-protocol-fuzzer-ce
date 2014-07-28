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
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Transformers.Encode
{
	[Description("Encode on output from a colon notation ipv6 address into a 16 byte octect representation.")]
	[Transformer("Ipv6StringToOctet", true)]
	[Transformer("encode.Ipv6StringToOctet")]
	[Serializable]
	public class Ipv6StringToOctet : Transformer
	{
		public Ipv6StringToOctet(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var reader = new BitReader(data);
			string sip = reader.ReadString();

			IPAddress ip;
			if (!IPAddress.TryParse(sip, out ip))
				throw new PeachException("Error, can't transform IP to bytes, '{0}' is not a valid IP address.".Fmt(sip));

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteBytes(ip.GetAddressBytes());
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			if (data.Length != 16)
				throw new PeachException("Error, can't transform bytes to IP, expected 16 bytes but got {0} bytes.".Fmt(data.Length));

			var reader = new BitReader(data);
			var bytes = reader.ReadBytes(16);
			IPAddress ip = new IPAddress(bytes);

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteString(ip.ToString());
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}

// end

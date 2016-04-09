
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
//   Mick Ayzenberg (mick@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers
{
	[Description("Truncates a Value")]
	[Transformer("Truncate", true)]
	[Parameter("Length", typeof(int), "Length to truncate in Bytes")]
	[Parameter("Offset", typeof(int), "Starting Index", "0")]
	[Serializable]
	public class Truncate : Transformer
	{
		Dictionary<string, Variant> m_args;

		public Truncate(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			m_args = args;
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			try
			{
				int maxlen = (int)data.Length;
				var dataAsBytes = new BitReader(data).ReadBytes(maxlen);
				int newlen = 0; 
				int start = 0;
				if (m_args.ContainsKey("Offset"))
					start = (int)m_args["Offset"];
				if (!m_args.ContainsKey("Length") || (int)m_args["Length"]+start > maxlen)
					newlen = maxlen - start;
				else
					newlen = (int)m_args["Length"]; 
				//var dataTrimmed = new byte[newlen];
				//Buffer.BlockCopy(dataAsBytes,start,dataTrimmed, 0, newlen);
				var ret = new BitStream();
				ret.Write(dataAsBytes, start, newlen);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end

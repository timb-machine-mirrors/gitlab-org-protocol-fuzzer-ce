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
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Encode on output as a URL with spaces turned to pluses.")]
    [Transformer("UrlEncode", true)]
    [Transformer("UrlEncodePlus")]
    [Transformer("encode.UrlEncode")]
    [Transformer("encode.UrlEncodePlus")]
    [Serializable]
    public class UrlEncode : Transformer
    {
        public UrlEncode(DataElement parent, Dictionary<string,Variant>  args)
            : base(parent, args)
        {
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
			byte[] buf = null;
			long startPosition = data.PositionBits;

			try
			{
				var str = new BitReader(data).ReadString();
				buf = System.Web.HttpUtility.UrlEncodeToBytes(str);
			}
			catch (System.Text.DecoderFallbackException)
			{
				data.PositionBits = startPosition;
				buf = new BitReader(data).ReadBytes((int)data.Length);
				buf = System.Web.HttpUtility.UrlEncodeToBytes(buf);
			}

            var ret = new BitStream();
            ret.Write(buf, 0, buf.Length);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }

        protected override BitStream internalDecode(BitStream data)
        {
            var str = new BitReader(data).ReadString();
            var buf = System.Web.HttpUtility.UrlDecodeToBytes(str);
            var ret = new BitStream();
            ret.Write(buf, 0, buf.Length);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end

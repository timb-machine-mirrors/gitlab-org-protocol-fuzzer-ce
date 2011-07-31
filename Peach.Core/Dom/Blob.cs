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
//   Michael Eddington (mike@phed.org)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Reflection;
using System.Runtime.Serialization;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Binary large object data element
	/// </summary>
	[DataElement("Blob")]
	[DataElementChildSupportedAttribute(DataElementTypes.NonDataElements)]
	[ParameterAttribute("length", typeof(uint), "Length in bytes", false)]
	[Serializable]
	public class Blob : DataElement
	{
		protected uint _length;

		public Blob()
		{
			_defaultValue = new Variant(new byte[] { });
		}
		
		public Blob(string name)
		{
			this.name = name;
			_defaultValue = new Variant(new byte[] { });
		}
		
		public Blob(string name, int length)
		{
			this.name = name;
			this.length = length;
			_defaultValue = new Variant(new byte[] { });
		}
		
		public Blob(string name, int length, Variant defaultValue)
		{
			this.name = name;
			this.length = length;
			_defaultValue = defaultValue;
		}
		
		public Blob(int length)
		{
			_defaultValue = new Variant(new byte[] { });
			this.length = length;
		}
		
		public Blob(int length, Variant defaultValue)
		{
			this.length = length;
			_defaultValue = defaultValue;
		}

		public Blob(Variant defaultValue)
		{
			_defaultValue = defaultValue;
		}
	}
}

// end

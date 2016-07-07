
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
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Provide scripting expression to perform fixup.")]
	[Fixup("Expression", true)]
	[Fixup("ExpressionFixup")]
	[Fixup("checksums.ExpressionFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("expression", typeof(string), "Expression returning string or int")]
	[Serializable]
	public class ExpressionFixup : Fixup
	{
		public ExpressionFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			if (!args.ContainsKey("expression"))
				throw new PeachException("Error, ExpressionFixup requires an 'expression' argument!");
		}

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var expression = (string)args["expression"];

			var state = new Dictionary<string, object>();
			state["self"] = this;
			state["ref"] = from;
			state["data"] = from.Value;
			try
			{
				object value = parent.EvalExpression(expression, state);

				if (value is byte[])
					return new Variant((byte[])value);

				if (value is string)
				{
					string str = value as string;
					byte[] strbytes = new byte[str.Length];

					for (int i = 0; i < strbytes.Length; ++i)
						strbytes[i] = (byte)str[i];

					return new Variant(strbytes);
				}

				if (value is int)
					return new Variant(Convert.ToInt32(value));

				throw new PeachException(
					"ExpressionFixup expected a return value of string or int but got '{0}'".Fmt(value.GetType().Name)
				);
			}
			catch (System.Exception ex)
			{
				throw new PeachException(
					"ExpressionFixup expression threw an exception!\nExpression: {0}\n Exception: {1}".Fmt(expression, ex.ToString()), ex
				);
			}
		}
	}
}

// end

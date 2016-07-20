
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
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

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

			object data;

			try
			{
				data = parent.EvalExpression(expression, state);
			}
			catch (Exception ex)
			{
				throw new PeachException(
					"ExpressionFixup expression threw an exception!\nExpression: {0}\n Exception: {1}".Fmt(expression, ex.ToString()), ex
				);
			}

			if (data == null)
				throw new PeachException("Error, expression fixup returned null.");

			var asVariant = Scripting.ToVariant(data);

			if (asVariant == null)
				throw new PeachException("Error, expression fixup returned unknown type '{0}'.".Fmt(data.GetType()));

			if (parent is Blob && asVariant.GetVariantType() == Variant.VariantType.String)
				return new Variant(Encoding.ISOLatin1.GetBytes((string)asVariant));

			return asVariant;
		}
	}
}

// end

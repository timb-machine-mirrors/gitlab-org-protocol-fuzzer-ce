
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
//   Ross Salpino (rsal42@gmail.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Peach.Core.Dom;
using System.Runtime.Serialization;
using Action = Peach.Core.Dom.Action;

namespace Peach.Core.Fixups
{
	[Description("Standard sequential increment fixup.")]
	[Fixup("SequenceIncrement", true)]
	[Fixup("SequenceIncrementFixup")]
	[Fixup("sequence.SequenceIncrementFixup")]
	[Parameter("Offset", typeof(uint?), "Sets the per-iteration initial value to Offset * (Iteration - 1)", "")]
	[Parameter("Once", typeof(bool), "Only increment once per iteration", "false")]
	[Serializable]
	public class SequenceIncrementFixup : VolatileFixup
	{
		public uint? Offset { get; private set; }
		public bool Once { get; private set; }

		public SequenceIncrementFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			if (!(parent is Dom.Number) && !(parent is Dom.String && parent.Hints.ContainsKey("NumericalString")))
				throw new PeachException("SequenceIncrementFixup has non numeric parent '" + parent.fullName + "'.");

			bool increment = true;
			ulong max = parent is Dom.Number ? ((Dom.Number)parent).MaxValue : ulong.MaxValue;
			ulong value = 0;
			object obj = null;

			string key = "SequenceIncrementFixup." + parent.fullName;

			if (ctx.stateStore.TryGetValue(key, out obj))
				value = (ulong)obj;

			if (ctx.iterationStateStore.ContainsKey(key))
				increment &= !Once;
			else if (Offset.HasValue)
				value = (ulong)Offset.Value * (ctx.currentIteration - 1);

			// For 2 bit number, offset is 2, 2 actions per iter:
			// Iter:  1a,1b,2a,2b,3a,3b,4a,4b,5a,5b,6a,6b
			// It-1:  0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5
			// Pre:   0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11
			// Want:  0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2
			// Final: 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3
			if (value > max)
				value = value % max;

			if (increment)
			{
				if (++value > max)
					value -= max;

				ctx.stateStore[key] = value;
				ctx.iterationStateStore[key] = value;
			}

			return new Variant(value);
		}
	}
}

// end

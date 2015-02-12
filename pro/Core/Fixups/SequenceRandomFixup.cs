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
//   Ross Salpino (rsal42@gmail.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard sequential random fixup.")]
	[Fixup("SequenceRandom", true)]
	[Fixup("SequenceRandomFixup")]
	[Fixup("sequence.SequenceRandomFixup")]
	[Serializable]
	public class SequenceRandomFixup : VolatileFixup
	{
		public SequenceRandomFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			Peach.Core.Dom.Number num = parent as Peach.Core.Dom.Number;
			if (num == null && !(parent is Peach.Core.Dom.String && parent.Hints.ContainsKey("NumericalString")))
				throw new PeachException("SequenceRandomFixup has non numeric parent '" + parent.fullName + "'.");

			object obj;

			if (ctx.controlRecordingIteration)
			{
				var dm = parent.root as DataModel;
				if (dm != null && dm.actionData != null)
				{
					// Allow value to be overridden via the stateStore using key:
					// Peach.VolatileOverride.StateName.ActionName.ModelName.Path.To.Element
					var key = "Peach.VolatileOverride.{0}.{1}".Fmt(dm.actionData.outputName, parent.fullName);

					if (ctx.stateStore.TryGetValue(key, out obj))
						return (Variant) obj;
				}
			}

			Random rng;

			if (!ctx.iterationStateStore.TryGetValue("SequenceRandomFixup", out obj))
			{
				rng = new Random(ctx.config.randomSeed + ctx.currentIteration);
				ctx.iterationStateStore.Add("SequenceRandomFixup", rng);
			}
			else
			{
				rng = obj as Random;
			}

			dynamic random;

			if (num != null)
			{
				if (num.Signed)
				{
					if (num.MaxValue == long.MaxValue)
						random = rng.NextInt64();
					else
						random = rng.Next((long)num.MinValue, (long)num.MaxValue + 1);
				}
				else
				{
					if (num.MaxValue == ulong.MaxValue)
						random = rng.NextUInt64();
					else
						random = rng.Next((ulong)num.MinValue, (ulong)num.MaxValue + 1);
				}
			}
			else
			{
				random = rng.NextInt32();
			}

			return new Variant(random);
		}
	}
}

// end

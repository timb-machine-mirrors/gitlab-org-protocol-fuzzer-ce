#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;
using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedDataVaranceMutator")]
	[Description("Change the length of sized data to count - N to count + N. Size indicator will stay the same.")]
	[Hint("SizedDataVaranceMutator-N", "Gets N by checking node for hint, or returns default (50).")]
	public class SizedDataVaranceMutator : SizedMutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedDataVaranceMutator(DataElement obj)
			: base("SizedDataVarianceMutator", obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		protected override bool OverrideRelation
		{
			get { return true; }
		}

		protected override List<long> GenerateValues(DataElement obj, int n)
		{
			// Find all numbers from [-n, n] where (originalDataLength + n) > 0
			// TODO: See if we want to exclude mutations where our size will be 0
			long min = (int)Math.Max(-(long)obj.InternalValue + 1, -n);

			var ret = new List<long>();
			while (min <= n)
				ret.Add(min++);

			return ret;
		}
	}
}

// end
#endif

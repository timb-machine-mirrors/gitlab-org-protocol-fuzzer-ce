#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.IO;
using Peach.Core.Dom;
using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedVaranceMutator")]
	[Description("Change the length of sizes to count - N to count + N")]
	[Hint("SizedVaranceMutator-N", "Gets N by checking node for hint, or returns default (50).")]
	public class SizedVaranceMutator : SizedMutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedVaranceMutator(DataElement obj)
			: base("SizedVaranceMutator", obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		protected override bool OverrideRelation
		{
			get { return false; }
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
#endif

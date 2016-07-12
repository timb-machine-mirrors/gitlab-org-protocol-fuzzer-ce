#if DISABLED


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedNumericalEdgeCasesMutator")]
	[Description("Change the length of sizes to numerical edge cases")]
	[Hint("SizedNumericalEdgeCasesMutator-N", "Gets N by checking node for hint, or returns default (50).")]
	public class SizedNumericalEdgeCasesMutator : SizedMutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedNumericalEdgeCasesMutator(DataElement obj)
			: base("SizedNumericalEdgeCasesMutator", obj)
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
			int size = 16;

			if ((obj is Number || obj is Flag) && (int)obj.lengthAsBits < 16)
				size = 8;

			// Ignore numbers where (originalDataLength + n) <= 0
			// TODO: max mono on n > 1000
			var bad = NumberGenerator.GenerateBadNumbers(size, n);
			var min = -(long)obj.InternalValue;
			var ret = bad.Where(a => min <= a).ToList();

			return ret;
		}
	}
}

// end
#endif

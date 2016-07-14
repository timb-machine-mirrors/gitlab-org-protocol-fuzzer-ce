#if DISABLED


using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("Change the length of sized data to numerical edge cases")]
	[Hint("SizedDataNumericalEdgeCasesMutator-N", "Gets N by checking node for hint, or returns default (50).")]
	public class SizedDataNumericalEdgeCasesMutator : SizedMutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedDataNumericalEdgeCasesMutator(DataElement obj)
			: base("SizedDataNumericalEdgeCasesMutator", obj)
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

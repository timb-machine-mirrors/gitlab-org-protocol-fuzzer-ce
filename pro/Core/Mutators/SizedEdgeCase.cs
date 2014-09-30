//
// Copyright (c) Deja vu Security
//

using System;
using System.Linq;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedEdgeCase")]
	[Description("Change the size and length of sized data to numerical edge cases")]
	public class SizedEdgeCase : SizedDataEdgeCase
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedEdgeCase(DataElement obj)
			: base(obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected override bool OverrideRelation
		{
			get
			{
				return false;
			}
		}
	}
}

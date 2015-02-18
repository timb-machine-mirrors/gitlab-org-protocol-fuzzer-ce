//
// Copyright (c) Deja vu Security
//

using NLog;
using Peach.Core;
using Peach.Core.Dom;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("SizedVariance")]
	[Description("Change the length of sized data to count - N to count + N.")]
	public class SizedVariance : SizedDataVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedVariance(DataElement obj)
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

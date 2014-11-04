//
// Copyright (c) Deja vu Security
//

using System;
using System.Linq;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("SizedDataEdgeCase")]
	[Description("Change the length of sized data to numerical edge cases")]
	public class SizedDataEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedDataEdgeCase(DataElement obj)
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

		protected virtual bool OverrideRelation
		{
			get
			{
				return true;
			}
		}

		protected override void GetLimits(DataElement obj, out long min, out ulong max)
		{
			var other = obj.relations.From<SizeRelation>().First().Of;

			min = 0;
			max = (ulong)Utility.SizedHelpers.MaxSize(other);

			// If we are a number, make sure our max is not larger than max long
			// since stream lengths are tracked as longs
			var asNum = obj as Dom.Number;
			if (asNum != null)
				max = Math.Min(max, asNum.MaxValue);
			else
				System.Diagnostics.Debug.Assert(obj is Dom.String);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Any mutable object with a size relation
			if (obj.isMutable && obj.relations.From<SizeRelation>().Any())
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			var rel = obj.relations.From<SizeRelation>().FirstOrDefault();

			if (rel != null)
			{
				var limit = Utility.SizedHelpers.MaxSize(rel.Of);
				if (value > limit)
				{
					logger.Trace("Skipping mutation, expansion to {0} would exceed max output size.", value);
					return;
				}
			}

			Utility.SizedHelpers.ExpandTo(obj, value, OverrideRelation);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}

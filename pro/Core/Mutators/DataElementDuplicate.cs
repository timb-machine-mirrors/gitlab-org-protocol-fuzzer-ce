//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Duplicate an element upto N times using a guassian distribution. Default N is 50..
	/// </summary>
	[Mutator("DataElementDuplicate")]
	[Description("Duplicate an element up to N times")]
	[Hint("DataElementDuplicate-N", "Standard deviation of number of duplications")]
	public class DataElementDuplicate : Mutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		uint variance;
		double stddev;

		public DataElementDuplicate(DataElement obj)
			: base(obj)
		{
			var limit = Utility.SizedHelpers.MaxDuplication(obj);
			variance = getN(obj, (uint)Math.Min(50, limit));

			// We generate 1/2 of a gaussian distribution so the
			// standard deviation is 1/3 of the variance
			stddev = variance / 3.0;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj.isMutable && obj.parent != null && !(obj is Flag) && !(obj is XmlAttribute) && obj.Value.LengthBits > 0)
				return true;

			return false;
		}

		public override int count
		{
			get { return (int)variance; }
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, mutation + 1);
		}

		public override void randomMutation(DataElement obj)
		{
			// Gaussian distribution, positive and centered on 1
			var next = context.Random.NextGaussian(0, stddev);
			var asUint = (uint)Math.Abs(Math.Round(next)) + 1;

			performMutation(obj, asUint);
		}

		void performMutation(DataElement obj, uint num)
		{
			var limit = Utility.SizedHelpers.MaxDuplication(obj);

			if (num > limit)
			{
				logger.Trace("Skipping mutation, duplication by {0} would exceed max output size.", num);
				return;
			}

			var idx = obj.parent.IndexOf(obj) + 1;
			var value = new BitStream();
			var src = obj.Value;

			src.CopyTo(value);
			src.SeekBits(0, SeekOrigin.Begin);
			value.SeekBits(0, SeekOrigin.Begin);

			var mutatedValue = new Variant(value);
			var baseName = obj.parent.UniqueName(obj.Name);

			for (int i = 0; i < num; ++i)
			{
				// Make sure we pick a unique name
				var newName = "{0}_{1}".Fmt(baseName, i);

				var newElem = (DataElement)Activator.CreateInstance(obj.GetType(), new object[] { newName });
				newElem.MutatedValue = mutatedValue;
				newElem.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;

				obj.parent.Insert(idx + i, newElem);
			}
		}
	}
}

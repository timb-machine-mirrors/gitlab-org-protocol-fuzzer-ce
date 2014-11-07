using Peach.Core.Dom;
using System;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Extension of StringLengthVariance that injects BOM characters randomly-ish into the strings.
	/// </summary>
	public abstract class StringBomLength : StringLengthVariance
	{
		public StringBomLength(DataElement obj)
			: base(obj)
		{
		}

		protected abstract override NLog.Logger Logger
		{
			get;
		}

		protected abstract byte[] BOM
		{
			get;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			base.performMutation(obj, value);

			// If the base didn't actually mutate the element, we shouldn't either
			if (obj.mutationFlags == MutateOverride.None)
				return;

			var val = obj.PreTransformedValue;

			// TODO: slice up and inject this.BOM and return a BitStreamList
			obj.MutatedValue = new Variant(val);
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}
	}
}

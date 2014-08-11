
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Text;

using Peach.Enterprise.Mutators.Utility;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Enterprise.Mutators
{
	[Mutator("StringLengthEdgeCase")]
	[Description("Produce Gaussian distributed string lengths around numerical edge cases.")]
	public class StringLengthEdgeCase : Mutator
	{
		long minValue;
		ulong maxValue;

		IntegerEdgeCases edgeCases;

		public StringLengthEdgeCase(DataElement obj)
		{
			name = "StringLengthEdgeCase";
			// TODO - Allow for 64 bit values!
			minValue = 0;
			maxValue = UInt16.MaxValue;

			edgeCases = new IntegerEdgeCases(minValue, maxValue);
		}

		public override uint mutation
		{
			// TODO - Make this work :)
			get { return 0; }
			set { }
		}

		public override int count
		{
			// TODO - Make this work :)
			get
			{ return 1000; }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			throw new NotSupportedException("TODO - Make this work");
		}

		public override void randomMutation(DataElement obj)
		{
			long len = (long)edgeCases.Next(context.Random);
			string value = (string) obj.DefaultValue;

			if (value.Length == len)
				return;

			if (value.Length < len)
				obj.MutatedValue = new Variant(value.Substring(0, (int)len));
			else
			{
				var sb = new StringBuilder((int)len);
				while ((sb.Length + value.Length) <= len)
					sb.Append(value);

				sb.Append(value.Substring(0, (int)len - sb.Length));
			}

			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

// end

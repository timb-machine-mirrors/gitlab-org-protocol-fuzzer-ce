//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
	[Mutator("NumberRandom")]
	[Description("Produce random number in range of underlying element.")]
	public class NumberRandom : Mutator
	{
		int total;
		Func<Variant> gen;

		public NumberRandom(DataElement obj)
			: base(obj)
		{
			var asNum = obj as Dom.Number;

			if (asNum == null)
			{
				System.Diagnostics.Debug.Assert(obj is Dom.String);

				total = 64;
				gen = () => new Variant(context.Random.Next(long.MinValue, long.MaxValue).ToString());
			}
			else if (asNum.Signed)
			{
				total = (int)asNum.lengthAsBits;
 
				if (asNum.lengthAsBits < 32)
					gen = () => new Variant(context.Random.Next((int)asNum.MinValue, (int)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 32)
					gen = () => new Variant(context.Random.NextInt32());
				else if (asNum.lengthAsBits < 64)
					gen = () => new Variant(context.Random.Next((long)asNum.MinValue, (long)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 64)
					gen = () => new Variant(context.Random.NextInt64());
				else
					throw new NotSupportedException();
			}
			else
			{
				total = (int)asNum.lengthAsBits;

				if (asNum.lengthAsBits < 32)
					gen = () => new Variant(context.Random.Next((uint)asNum.MinValue, (uint)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 32)
					gen = () => new Variant(context.Random.NextUInt32());
				else if (asNum.lengthAsBits < 64)
					gen = () => new Variant(context.Random.Next((ulong)asNum.MinValue, (ulong)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 64)
					gen = () => new Variant(context.Random.NextUInt64());
				else
					throw new NotSupportedException();
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			// Ignore numbers <= 8 bits, they will be mutated
			// with the NumericalVariance mutator
			return obj is Dom.Number && obj.isMutable && obj.lengthAsBits > 8;
		}

		public override int count
		{
			get
			{
				return total;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			// Same as random, but seed is fixed
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			obj.MutatedValue = gen();
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

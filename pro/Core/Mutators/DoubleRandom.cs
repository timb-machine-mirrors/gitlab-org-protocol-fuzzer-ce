using System;

using Peach.Core.Dom;

using NLog;

namespace Peach.Core.Mutators
{
    //Hide this mutator as its not fully tested
    [Mutator("DoubleRandom")]
	[Description("Produce random number in range of underlying element.")]
	public class DoubleRandom : Mutator
    {
        const int maxCount = 5000; // Maximum count is 5000

		Func<Variant> gen;

        public DoubleRandom(DataElement obj)
			: base(obj)
		{
            var asNum = obj as Peach.Core.Dom.Double;

            if (asNum.lengthAsBits == 32)
                gen = () => new Variant(BitConverter.ToDouble(BitConverter.GetBytes(context.Random.NextUInt32()), 0));
            else if (asNum.lengthAsBits == 64)
                gen = () => new Variant(BitConverter.ToDouble(BitConverter.GetBytes(context.Random.NextUInt64()), 0));
            else
                throw new NotSupportedException();

            if (asNum == null)
            {
                System.Diagnostics.Debug.Assert(obj is Dom.String);

                if (asNum.lengthAsBits == 32)
                    gen = () => new Variant(BitConverter.ToDouble(BitConverter.GetBytes(context.Random.NextUInt32()), 0).ToString());
                else if (asNum.lengthAsBits == 64)
                    gen = () => new Variant(BitConverter.ToDouble(BitConverter.GetBytes(context.Random.NextUInt64()), 0).ToString());
            }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

            return obj is Peach.Core.Dom.Double && obj.isMutable && (obj.lengthAsBits == 64 || obj.lengthAsBits == 32);
		}

		public override int count
		{
			get
			{
                return maxCount;
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

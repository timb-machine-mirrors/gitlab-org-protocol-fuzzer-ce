using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    //Hide this mutator as its not fully tested
    [Mutator("DoubleVariance")]
    [Description("Produce random number in range of underlying element.")]
    public class DoubleVariance : Mutator
    {
        const int maxCount = 5000; // Maximum count is 5000

        public DoubleVariance(DataElement obj)
            : base(obj)
        {
        }

        public new static bool supportedDataElement(DataElement obj)
        {
            if (obj is Dom.String && obj.isMutable)
                return obj.Hints.ContainsKey("NumericalString");

            var asDouble = obj as Peach.Core.Dom.Double;
            if (asDouble != null)
            {
                bool supported = false;

                supported = (double)asDouble.DefaultValue != double.NegativeInfinity;
                supported = supported && (double)asDouble.DefaultValue != double.PositiveInfinity;
                supported = supported && (double)asDouble.DefaultValue != double.NaN;

                return obj.isMutable && supported;
            }


            return false;
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
            randomMutation(obj);
		}
        public override void randomMutation(DataElement obj)
        {
            double sigma;
            if (obj.lengthAsBits == 32)
                sigma = float.MaxValue / 3;
            else
                sigma = double.MaxValue / 3;

            var val = context.Random.NextGaussian(0, sigma);

            var def = (double)obj.DefaultValue;

            if ((obj.lengthAsBits == 32 && def == float.MaxValue)
                || (obj.lengthAsBits == 64 && def == double.MaxValue))
                val = unchecked(def - Math.Abs(val));
            else if ((obj.lengthAsBits == 32 && def == float.MinValue)
                || (obj.lengthAsBits == 64 && def == double.MinValue))
                val = unchecked(def + Math.Abs(val));

            obj.MutatedValue = new Variant(val);
            obj.mutationFlags = MutateOverride.Default;
        }
    }
}

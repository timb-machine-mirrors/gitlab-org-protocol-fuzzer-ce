#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Mutators
{
    [Mutator("UnicodeBomMutator")]
    [Description("Injects BOM markers into default value and longer strings")]
    public partial class UnicodeBomMutator : Mutator
    {
        // members
        //
        uint pos = 0;

        // CTOR
        //
        public UnicodeBomMutator(DataElement obj)
        {
            pos = 0;
            name = "UnicodeBomMutator";
        }

        // COUNT
        //
        public override int count
        {
            get { return values.Length; }
        }

        public override uint mutation
        {
            get { return pos; }
	    set { pos = value; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            Hint hint;
            if (obj.Hints.TryGetValue("Peach.TypeTransform", out hint))
                if (hint.Value.ToLower() == "false")
                    return false;

            if ((obj is Dom.String) && obj.isMutable)
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            var item = values[pos];
            var bs = new BitStream();
            bs.Write(item, 0, item.Length);
            obj.MutatedValue = new Variant(bs);

            obj.mutationFlags = MutateOverride.Default;
            obj.mutationFlags |= MutateOverride.TypeTransform;
        }

        // RANDOM_MUTATION
        //
        public override void randomMutation(DataElement obj)
        {
            var item = context.Random.Choice(values);
            var bs = new BitStream();
            bs.Write(item, 0, item.Length);
            obj.MutatedValue = new Variant(bs);

            obj.mutationFlags = MutateOverride.Default;
            obj.mutationFlags |= MutateOverride.TypeTransform;
        }
    }
}

// end
#endif

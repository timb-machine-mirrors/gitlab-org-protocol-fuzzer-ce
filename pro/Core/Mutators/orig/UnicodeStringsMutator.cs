#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("UnicodeStringsMutator")]
    [Description("Perform common unicode string mutations")]
    public partial class UnicodeStringsMutator : Mutator
    {
        // members
        //
        uint pos = 0;

        // CTOR
        //
        public UnicodeStringsMutator(DataElement obj)
        {
            pos = 0;
            name = "UnicodeStringsMutator";
        }

        // MUTATION
        //
        public override uint mutation
        {
            get { return pos; }
            set { pos = value; }
        }

        // COUNT
        //
        public override int count
        {
            get { return values.Length; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            if (obj is Dom.String && obj.isMutable && ((Dom.String)obj).stringType != StringType.ascii)
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            applyMutation(obj, values[pos]);
        }

        // RANDOM_MUTATION
        //
        public override void randomMutation(DataElement obj)
        {
            applyMutation(obj, context.Random.Choice(values));
        }

        private void applyMutation(DataElement obj, string value)
        {
            obj.mutationFlags = MutateOverride.Default;
            obj.MutatedValue = new Variant(value);
        }
    }
}

// end
#endif

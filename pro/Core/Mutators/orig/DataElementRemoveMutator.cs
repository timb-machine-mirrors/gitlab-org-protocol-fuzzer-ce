#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("DataElementRemoveMutator")]
    [Description("Remove nodes from a data tree")]
    public class DataElementRemoveMutator : Mutator
    {
        // CTOR
        //
        public DataElementRemoveMutator(DataElement obj)
        {
            name = "DataElementRemoveMutator";
        }

        // MUTATION
        //
        public override uint mutation
        {
            get { return 0; }
            set { }
        }

        // COUNT
        //
        public override int count
        {
            get { return 1; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            if (obj.isMutable && obj.parent != null && !(obj is Flag))
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            // remove the element from the data model
            obj.parent.Remove(obj);
            obj.mutationFlags = MutateOverride.Default;
        }

        // RANDOM_MUTAION
        //
        public override void randomMutation(DataElement obj)
        {
            obj.parent.Remove(obj);
            obj.mutationFlags = MutateOverride.Default;
        }
    }
}

// end
#endif

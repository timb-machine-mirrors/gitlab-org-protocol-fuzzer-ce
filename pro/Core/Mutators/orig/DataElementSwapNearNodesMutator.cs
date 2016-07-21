#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("DataElementSwapNearNodesMutator")]
    [Description("Swap two nodes in the data model that are near each other")]
    public class DataElementSwapNearNodesMutator : Mutator
    {
        // CTOR
        //
        public DataElementSwapNearNodesMutator(DataElement obj)
        {
            name = "DataElementSwapNearNodesMutator";
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
            if (obj.isMutable && obj.parent != null && !(obj is Flag) && obj.nextSibling() != null)
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            var dataModel = (DataElementContainer)obj.parent;
            int idx1 = dataModel.IndexOf(obj);
            int idx2 = idx1 + 1;
            int count = dataModel.Count;

            if (idx2 < count)
                dataModel.SwapElements(idx1, idx2);
        }

        // RANDOM_MUTAION
        //
        public override void randomMutation(DataElement obj)
        {
            sequentialMutation(obj);
        }
    }
}

// end
#endif

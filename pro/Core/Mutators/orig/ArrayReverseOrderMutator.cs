#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Mutators
{
    [Mutator("ArrayReverseOrderMutator")]
    [Description("Reverse the order of the array")]
    public class ArrayReverseOrderMutator : Mutator
    {
        // CTOR
        //
        public ArrayReverseOrderMutator(DataElement obj)
        {
            name = "ArrayReverseOrderMutator";
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
            if (obj is Dom.Array && obj.isMutable)
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            performMutation(obj);
            obj.mutationFlags = MutateOverride.Default;
        }

        // RANDOM_MUTAION
        //
        public override void randomMutation(DataElement obj)
        {
            performMutation(obj);
            obj.mutationFlags = MutateOverride.Default;
        }

        // PERFORM_MUTATION
        //
        private void performMutation(DataElement obj)
        {
            Dom.Array objAsArray = (Dom.Array)obj;
            List<DataElement> items = new List<DataElement>();

            for (int i = 0; i < objAsArray.Count; ++i)
                items.Add(objAsArray[i]);

            items.Reverse();
            objAsArray.Clear();

            for (int i = 0; i < items.Count; ++i)
                objAsArray.Add(items[i]);
        }
    }
}

// end
#endif

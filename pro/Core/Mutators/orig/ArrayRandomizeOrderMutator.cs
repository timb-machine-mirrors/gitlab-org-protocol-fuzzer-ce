#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("ArrayRandomizeOrderMutator")]
    [Description("Randomize the order of the array")]
    [Hint("ArrayRandomizeOrderMutator-N", "Gets N by checking node for hint, or returns default (50).")]
    public class ArrayRandomizeOrderMutator : Mutator
    {
        // members
        //
        uint currentCount;
        int n;

        // CTOR
        //
        public ArrayRandomizeOrderMutator(DataElement obj)
            : base(obj)
        {
            name = "ArrayRandomizeOrderMutator";
            currentCount = 0;
            n = getN(obj, 50);
        }

        // GET N
        //
        public int getN(DataElement obj, int n)
        {
            // check for hint
            if (obj.Hints.ContainsKey(name + "-N"))
            {
                Hint h = null;
                if (obj.Hints.TryGetValue(name + "-N", out h))
                {
                    try
                    {
                        n = Int32.Parse(h.Value);
                    }
                    catch (Exception ex)
                    {
                        throw new PeachException("Expected numerical value for Hint named " + h.Name, ex);
                    }
                }
            }

            return n;
        }

        // MUTATION
        //
        public override uint mutation
        {
            get { return currentCount; }
            set { currentCount = value; }
        }

        // COUNT
        //
        public override int count
        {
            get { return n; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            var array = obj as Dom.Array;

            if ( array != null && array.isMutable && array.Count > 1 )
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            // Only called via the Sequential mutation strategy, which should always have a consistent seed

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

            var shuffledItems = context.Random.Shuffle(items.ToArray());
            objAsArray.Clear();

            for (int i = 0; i < shuffledItems.Length; ++i)
                objAsArray.Add(shuffledItems[i]);
        }
    }
}

// end
#endif

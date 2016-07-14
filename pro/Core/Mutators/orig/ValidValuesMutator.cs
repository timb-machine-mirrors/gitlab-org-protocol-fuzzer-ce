#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("ValidValuesMutator")]
    [Description("Allows different valid values to be specified")]
    [Hint("ValidValues", "Provide additional values for element separeted with ;.")]
    public class ValidValuesMutator : Mutator
    {
        // members
        //
        uint pos = 0;
        string[] values = new string[] { };

        // CTOR
        //
        public ValidValuesMutator(DataElement obj)
        {
            pos = 0;
            name = "ValidValuesMutator";
            generateValues(obj);
        }

        // GENERATE VALUES
        //
        public void generateValues(DataElement obj)
        {
            // 1. Get hint
            // 2. Split on ';'
            // 3. Return each value in turn

            Hint h = null;
            if (obj.Hints.TryGetValue("ValidValues", out h))
            {
                values = h.Value.Split(';');
            }
        }


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
            if ((obj is Dom.String || obj is Dom.Number || obj is Dom.Blob) && obj.isMutable)
            {
                if (obj.Hints.ContainsKey("ValidValues"))
                    return true;
            }

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            obj.MutatedValue = new Variant(values[pos]);
            obj.mutationFlags = MutateOverride.Default;
        }

        // RANDOM_MUTATION
        //
        public override void randomMutation(DataElement obj)
        {
            obj.MutatedValue = new Variant(context.Random.Choice(values));
            obj.mutationFlags = MutateOverride.Default;
        }
    }
}

// end
#endif

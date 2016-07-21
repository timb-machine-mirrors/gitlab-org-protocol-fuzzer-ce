#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("FiniteRandomNumbersMutator")]
    [Description("Produce a finite number of random numbers for each <Number> element")]
    [Hint("FiniteRandomNumbersMutator-N", "Gets N by checking node for hint, or returns default (5000).")]
    public class FiniteRandomNumbersMutator : Mutator
    {
        // members
        //
        int n;
        int size;
        bool signed;
        uint currentCount;
        long minValue;
        ulong maxValue;

        // CTOR
        //
        public FiniteRandomNumbersMutator(DataElement obj)
        {
            name = "FiniteRandomNumbersMutator";
            currentCount = 0;
            n = getN(obj, 5000);

            if (obj is Dom.String)
            {
                signed = false;
                size = 32;
                minValue = 0;
                maxValue = UInt32.MaxValue;
            }
            else if (obj is Number || obj is Flag)
            {
                signed = ((Number)obj).Signed;
                size = (int)((Number)obj).lengthAsBits;
                minValue = ((Number)obj).MinValue;
                maxValue = ((Number)obj).MaxValue;
            }
        }

        // GET N
        //
        public int getN(DataElement obj, int n)
        {
            // check for hint
            if (obj.Hints.ContainsKey("FiniteRandomNumbersMutator-N"))
            {
                Hint h = null;
                if (obj.Hints.TryGetValue("FiniteRandomNumbersMutator-N", out h))
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
            // Ignore numbers <= 8 bits, they will be mutated with the
            // NumericalEdgeCaseMutator


            if (obj is Number && obj.isMutable)
                if (((Number)obj).lengthAsBits > 8)
                    return true;

            if (obj is Flag && obj.isMutable)
                if (((Flag)obj).lengthAsBits > 8)
                    return true;

            if (obj is Dom.String && obj.isMutable)
                if (obj.Hints.ContainsKey("NumericalString"))
                    return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            // Only called via the Sequential mutation strategy, which should always have a consistent seed
            randomMutation(obj);
        }

        // RANDOM_MUTAION
        //
        public override void randomMutation(DataElement obj)
        {
            // handle strings
            if (obj is Dom.String)
            {
                UInt32 value = context.Random.NextUInt32();
                obj.MutatedValue = new Variant(value.ToString());
                obj.mutationFlags = MutateOverride.Default;
                return;
            }

            dynamic val;

            if (signed)
            {
                if (size < 32)
                    val = context.Random.Next((int)minValue, (int)maxValue);
                else if (size == 32)
                    val = context.Random.NextInt32();
                else if (size < 64)
                    val = context.Random.Next((long)minValue, (long)maxValue);
                else
                    val = context.Random.NextInt64();
            }
            else
            {
                if (size < 32)
                    val = context.Random.Next((uint)minValue, (uint)maxValue);
                else if (size == 32)
                    val = context.Random.NextUInt32();
                else if (size < 64)
                    val = context.Random.Next((ulong)minValue, (ulong)maxValue);
                else
                    val = context.Random.NextUInt64();
            }

            obj.MutatedValue = new Variant(val);
            obj.mutationFlags = MutateOverride.Default;
        }
    }
}

// end
#endif

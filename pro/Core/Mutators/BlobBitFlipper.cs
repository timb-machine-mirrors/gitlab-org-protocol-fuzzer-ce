using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Peach.Core.Dom;
using Peach.Core.IO;
using NLog;

namespace Peach.Core.Mutators
{
    [Mutator("BlobBitFlipperMutator")]
    [Description("Flip a % of total bits in a blob. Default is 20%.")]
    [Hint("BlobBitFlipperMutator-N", "Gets N by checking node for hint, or returns default (20).")]
    public class BlobBitFlipperMutator : Mutator
    {
        int n;
        int countMax;
        uint current;
        long length;

        public BlobBitFlipperMutator(DataElement obj)
        {
            current = 0;
            n = getN(obj, 20);
            length = obj.Value.LengthBits;
            name = "BlobBitFlipperMutator";

            if (n != 0)
                countMax = (int)((length) * (n / 100.0));
            else
                countMax = (int)((length) * 0.2);
        }

        public int getN(DataElement obj, int n)
        {
            // check for hint
            if (obj.Hints.ContainsKey("BlobBitFlipperMutator-N"))
            {
                Hint h = null;
                if (obj.Hints.TryGetValue("BlobBitFlipperMutator-N", out h))
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

        public override uint mutation
        {
            get { return current; }
            set { current = value; }
        }

        public override int count
        {
            get { return countMax; }
        }

        public new static bool supportedDataElement(DataElement obj)
        {
            if (obj is Dom.Blob && obj.isMutable)
                return true;

            return false;
        }

        public override void sequentialMutation(DataElement obj)
        {
            // Only called via the Sequential mutation strategy, which should always have a consistent seed

            randomMutation(obj);
        }

        public override void randomMutation(DataElement obj)
        {
            var data = obj.Value;

            if (data.Length == 0)
                return;

            // pick a random bit
            long bit = context.Random.Next(data.LengthBits);

            // seek, read, rewind
            data.SeekBits(bit, SeekOrigin.Begin);
            var value = data.ReadBit();
            data.SeekBits(bit, SeekOrigin.Begin);

            // flip
            if (value == 0)
                data.WriteBit(1);
            else
                data.WriteBit(0);

            data.Seek(0, SeekOrigin.Begin);

            obj.MutatedValue = new Variant(data);
            obj.mutationFlags = MutateOverride.Default;
            obj.mutationFlags |= MutateOverride.TypeTransform;
        }
    }
}

// end

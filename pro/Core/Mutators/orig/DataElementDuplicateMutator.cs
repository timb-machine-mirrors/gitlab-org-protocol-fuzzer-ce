#if DISABLED


using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("DataElementDuplicateMutator")]
    [Description("Duplicate a node's value starting from 1x - 50x")]
    public class DataElementDuplicateMutator : Mutator
    {
        // members
        //
        uint currentCount;
        int minCount;
        int maxCount;

        // CTOR
        //
        public DataElementDuplicateMutator(DataElement obj)
        {
            minCount = 1;
            maxCount = 50;
            currentCount = (uint)minCount;
            name = "DataElementDuplicateMutator";
        }

        // MUTATION
        //
        public override uint mutation
        {
            get { return currentCount - (uint)minCount; }
            set { currentCount = value + (uint)minCount; }
        }

        // COUNT
        //
        public override int count
        {
            get { return maxCount - minCount; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            if (obj.isMutable && obj.parent != null && !(obj is Flag) && !(obj is XmlAttribute))
                return true;

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            performMutation(obj, currentCount);
        }

        // RANDOM_MUTAION
        //
        public override void randomMutation(DataElement obj)
        {
            uint newCount = (uint)context.Random.Next(minCount, maxCount + 1);
            performMutation(obj, newCount);
        }

		private void performMutation(DataElement obj, uint newCount)
		{
			int startIdx = obj.parent.IndexOf(obj) + 1;
			var value = new Peach.Core.IO.BitStream();
			var src = obj.Value;
			src.CopyTo(value);
			src.SeekBits(0, System.IO.SeekOrigin.Begin);
			value.SeekBits(0, System.IO.SeekOrigin.Begin);
			var mutatedValue = new Variant(value);

			var baseName = obj.parent.UniqueName(obj.name);

			for (int i = 0; i < newCount; ++i)
			{
				// Make sure we pick a unique name
				string newName = "{0}_{1}".Fmt(baseName, i);

				DataElement newElem = Activator.CreateInstance(obj.GetType(), new object[] { newName }) as DataElement;
				newElem.MutatedValue = mutatedValue;
				newElem.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;

				obj.parent.Insert(startIdx + i, newElem);
			}
		}

    }
}

// end
#endif

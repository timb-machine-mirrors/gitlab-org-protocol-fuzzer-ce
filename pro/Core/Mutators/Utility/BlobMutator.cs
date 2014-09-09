//
// Copyright (c) Deja vu Security
//

using System;

using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Mutators.Utility
{
	/// <summary>
	/// Generate integer edge cases. The numbers produced are distributed 
	/// over a bell curve with the edge case as the center.
	/// </summary>
	public abstract class BlobMutator : Mutator
	{
		public BlobMutator(DataElement obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Dom.Blob && obj.isMutable)
				return true;

			return false;
		}

		public sealed override uint mutation
		{
			get;
			set;
		}

		public sealed override int count
		{
			get
			{
				return 0;
			}
		}

		public sealed override void sequentialMutation(DataElement obj)
		{
		}

		public sealed override void randomMutation(DataElement obj)
		{
		}

		protected abstract long MaxLength { get; }

		protected abstract bool ClampLength { get; }

		/// <summary>
		/// Perform mutation of a sequence of bytes.
		/// </summary>
		/// <remarks>
		/// The 'start' argument will always fall within the length of the bit stream.
		/// The 'length' argument can potentially run past the end of the bit stream
		/// if 'ClampLength' is false.  When 'ClampLength' is true, 'length' will never
		/// run past the end of the bit stream.
		/// </remarks>
		/// <param name="data">The source data to alter.</param>
		/// <param name="start">The start position to begin altering.</param>
		/// <param name="length">The number of bytes to alter.</param>
		/// <returns>The altered data.</returns>
		protected abstract BitwiseStream PerformMutation(BitStream data, long start, long length);
	}
}

